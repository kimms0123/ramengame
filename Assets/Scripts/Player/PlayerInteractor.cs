using System.Collections.Generic;
using RamenTycoon.Kitchen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RamenTycoon.Player
{
    /// <summary>
    /// 주변 상호작용 대상을 찾아 하이라이트하고, Space/E/게임패드 A로 상호작용한다.
    /// 선택 기준: 바라보는 방향 앞쪽 원 안에서 (거리 - 방향 일치도) 점수가 가장 낮은 대상.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(PlayerHand))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float probeDistance = 0.55f;
        [SerializeField] float probeRadius = 0.6f;
        [SerializeField, Range(0f, 2f)] float facingWeight = 0.6f;

        PlayerController controller;
        PlayerHand hand;
        InputAction interactAction;
        ContactFilter2D filter;
        readonly List<Collider2D> hits = new();

        public IInteractable Current { get; private set; }

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            hand = GetComponent<PlayerHand>();

            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/space");
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");

            filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
        }

        void OnEnable() => interactAction.Enable();
        void OnDisable() => interactAction.Disable();
        void OnDestroy() => interactAction.Dispose();

        void Update()
        {
            UpdateTarget();

            if (interactAction.WasPressedThisFrame() && Current != null && Current.CanInteract(hand))
            {
                Current.Interact(hand);
            }
        }

        void UpdateTarget()
        {
            Vector2 origin = (Vector2)transform.position + controller.Facing * probeDistance;
            hits.Clear();
            Physics2D.OverlapCircle(origin, probeRadius, filter, hits);

            IInteractable best = null;
            float bestScore = float.MaxValue;

            foreach (var col in hits)
            {
                if (col.attachedRigidbody != null && col.attachedRigidbody.gameObject == gameObject) continue;
                var target = col.GetComponentInParent<IInteractable>();
                if (target == null) continue;

                Vector2 toTarget = (Vector2)col.bounds.center - (Vector2)transform.position;
                float dot = Vector2.Dot(controller.Facing, toTarget.normalized);
                float score = toTarget.magnitude - dot * facingWeight;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }

            if (!ReferenceEquals(best, Current))
            {
                Current?.SetHighlighted(false);
                Current = best;
                Current?.SetHighlighted(true);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector2 facing = Application.isPlaying && controller != null ? controller.Facing : Vector2.down;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere((Vector2)transform.position + facing * probeDistance, probeRadius);
        }
    }
}