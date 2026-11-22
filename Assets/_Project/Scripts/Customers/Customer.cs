using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using UnityEngine;

namespace RamenTycoon.Customers
{
    /// <summary>
    /// 손님. 출입문 → 통로 → 테이블 자리로 걸어가서 주문하고, 식사 후(또는 화가 나서) 같은 길로 나간다.
    /// 물리 충돌이 없어서 플레이어 동선을 막지 않는다.
    /// 경로는 출입문 → 홀 가운데 통로(aisleY) → 자리 의 꺾은선. (나중에 A*로 교체 가능)
    /// </summary>
    public class Customer : MonoBehaviour
    {
        enum MoveState { WalkingIn, Seated, Leaving }

        public static readonly List<Customer> Active = new();

        [SerializeField] float speed = 5.5f;
        [SerializeField] SpriteRenderer body;
        [SerializeField] Animator animator;

        static readonly Color AngryTint = new(1f, 0.55f, 0.55f);
        static readonly Color HappyTint = new(1f, 1f, 0.85f);

        readonly List<Vector3> path = new();
        int pathIndex;
        MoveState state;
        Table table;
        Color baseColor = Color.white;
        bool hasMovingParam;
        CharacterAnimator charAnim;
        System.Action<Customer, Table> onArrived;

        public bool IsLeaving => state == MoveState.Leaving;
        public SpriteRenderer Body => body;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void Init(Table target, Vector3 spawn, Vector3 door, float aisleY, System.Action<Customer, Table> arrived)
        {
            table = target;
            onArrived = arrived;
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (body != null) baseColor = body.color;
            charAnim = GetComponent<CharacterAnimator>();
            CacheAnimatorParams();

            Vector3 seat = target.SeatPosition;
            path.Clear();
            path.Add(door);
            path.Add(new Vector3(door.x, aisleY, 0f));
            path.Add(new Vector3(seat.x, aisleY, 0f));
            path.Add(seat);

            transform.position = spawn;
            pathIndex = 0;
            state = MoveState.WalkingIn;
            target.Reserve(this);

            // 나갈 때는 같은 길을 거꾸로 + 원래 스폰 위치
            exitPath.Clear();
            for (int i = path.Count - 2; i >= 0; i--) exitPath.Add(path[i]);
            exitPath.Add(spawn);
        }

        readonly List<Vector3> exitPath = new();

        public void Leave(bool angry)
        {
            if (state == MoveState.Leaving) return;
            state = MoveState.Leaving;
            path.Clear();
            path.AddRange(exitPath);
            pathIndex = 0;
            if (body != null && angry) body.color = baseColor * AngryTint;
        }

        public void ShowMood(float score)
        {
            if (body == null) return;
            body.color = score >= 70f ? baseColor * HappyTint : score <= 0f ? baseColor * AngryTint : baseColor;
        }

        void Update()
        {
            if (state == MoveState.Seated)
            {
                SetMoving(false);
                if (charAnim != null) charAnim.FacingOverride = Facing.Front; // 자리에서는 앞을 봄
                return;
            }
            if (charAnim != null) charAnim.FacingOverride = null;

            if (pathIndex >= path.Count)
            {
                if (state == MoveState.WalkingIn)
                {
                    state = MoveState.Seated;
                    onArrived?.Invoke(this, table);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            Vector3 target = path[pathIndex];
            Vector3 pos = transform.position;
            Vector3 next = Vector3.MoveTowards(pos, target, speed * GameClock.DeltaTime);
            transform.position = next;

            float dx = next.x - pos.x;
            // 4방향 애니메이션이 있으면 그쪽에서 방향을 정한다
            if (charAnim == null && body != null && Mathf.Abs(dx) > 0.0001f) body.flipX = dx < 0f;
            SetMoving(GameClock.DeltaTime > 0f);

            if ((next - target).sqrMagnitude < 0.0004f) pathIndex++;
        }

        void CacheAnimatorParams()
        {
            hasMovingParam = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (var p in animator.parameters)
            {
                if (p.name == "IsMoving" && p.type == AnimatorControllerParameterType.Bool) hasMovingParam = true;
            }
        }

        void SetMoving(bool moving)
        {
            if (hasMovingParam) animator.SetBool("IsMoving", moving);
        }
    }
}
