using RamenTycoon.Audio;
using System.Collections;
using RamenTycoon.Core;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 쓰레기통. 잘못 만든 음식을 버린다.
    /// - 음식이 담긴 그릇 → 내용물만 버리고 손에는 깨끗한 빈 그릇이 남음 (Keep Bowl을 끄면 그릇째 버림)
    /// - 손에 든 재료(면 등) → 버림
    /// - 이미 들어간 재료비는 돌려받지 않는다
    /// </summary>
    public class TrashCan : Station
    {
        [Tooltip("켜면 내용물만 버리고 빈 그릇은 손에 남김. 끄면 그릇째 버림")]
        [SerializeField] bool keepBowl = true;

        [Tooltip("버릴 때 쓰레기통이 들썩이는 정도")]
        [SerializeField] float bounce = 0.12f;

        protected override Sprite DefaultSprite(Data.GameArtConfig art) => art.trashCan;
        protected override float DefaultWidth => 1.15f;

        Coroutine bounceRoutine;
        Vector3 bodyBaseScale = Vector3.one;

        protected override void Awake()
        {
            base.Awake();
            if (bodyRenderer != null) bodyBaseScale = bodyRenderer.transform.localScale;
        }

        public override bool CanInteract(PlayerHand hand)
        {
            if (hand.Held is IngredientItem) return true;
            return hand.Held is Bowl bowl && !bowl.IsDirty && !bowl.IsEmpty;
        }

        public override string GetHint(PlayerHand hand)
        {
            if (hand.IsEmpty) return "잘못 만든 음식을 들고 오면 버릴 수 있어요";
            if (hand.Held is Bowl bowl)
            {
                if (bowl.IsDirty) return "더러운 그릇은 싱크대에 넣어요";
                if (bowl.IsEmpty) return "빈 그릇이라 버릴 게 없어요";
            }
            return null;
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (hand.Held is Bowl bowl)
            {
                if (OrderBoard.Instance != null) OrderBoard.Instance.Unassign(bowl); // 주문서에 그어진 줄도 되돌림
                if (keepBowl) bowl.Empty();
                else Destroy(hand.Release().gameObject);
            }
            else
            {
                Destroy(hand.Release().gameObject);
            }

            AudioManager.Play("trash");

            if (bodyRenderer != null && bounce > 0f)
            {
                if (bounceRoutine != null) StopCoroutine(bounceRoutine);
                bounceRoutine = StartCoroutine(Bounce(bodyRenderer.transform));
            }
        }

        IEnumerator Bounce(Transform t)
        {
            Vector3 baseScale = bodyBaseScale;
            float time = 0f;
            const float duration = 0.25f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = 1f + Mathf.Sin(time / duration * Mathf.PI) * bounce;
                t.localScale = new Vector3(baseScale.x * (2f - k), baseScale.y * k, baseScale.z);
                yield return null;
            }
            t.localScale = baseScale;
            bounceRoutine = null;
        }
    }
}