using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Core
{
    public enum Facing { Front, Back, Left, Right }

    /// <summary>
    /// 4방향 스프라이트 애니메이션 (Animator 없이 스프라이트만 바꾼다).
    /// 이동 방향으로 앞/뒤/옆을 고르고, 움직이면 걷기 프레임을, 멈추면 서 있는 프레임을 보여준다.
    /// Rigidbody2D가 있으면 속도로, 없으면 위치 변화로 이동을 판단한다 (손님은 위치로).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        [SerializeField] CharacterSpriteSet sprites;
        [SerializeField] SpriteRenderer body;
        [SerializeField] float moveThreshold = 0.05f;

        Rigidbody2D rb;
        Vector3 lastPosition;
        float frameTimer;
        int walkIndex;

        public Facing Facing { get; private set; } = Facing.Front;
        public bool IsMoving { get; private set; }

        /// <summary>값을 넣으면 그 방향으로 고정 (예: 자리에 앉은 손님은 앞을 봄)</summary>
        public Facing? FacingOverride { get; set; }

        public void Setup(CharacterSpriteSet set, SpriteRenderer renderer)
        {
            sprites = set;
            body = renderer;
            Apply();
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            lastPosition = transform.position;
        }

        void LateUpdate()
        {
            if (sprites == null || !sprites.IsValid || body == null) return;

            Vector2 v;
            if (rb != null) v = rb.linearVelocity;
            else
            {
                float dt = Time.deltaTime;
                v = dt > 0f ? (Vector2)(transform.position - lastPosition) / dt : Vector2.zero;
                lastPosition = transform.position;
            }

            IsMoving = v.magnitude > moveThreshold && GameClock.DeltaTime > 0f;
            if (IsMoving)
            {
                if (Mathf.Abs(v.x) > Mathf.Abs(v.y) * 1.1f) Facing = v.x < 0f ? Facing.Left : Facing.Right;
                else Facing = v.y > 0f ? Facing.Back : Facing.Front;
            }
            if (FacingOverride.HasValue && !IsMoving) Facing = FacingOverride.Value;

            if (IsMoving)
            {
                frameTimer += GameClock.DeltaTime;
                float frame = 1f / Mathf.Max(1f, sprites.walkFps);
                while (frameTimer >= frame)
                {
                    frameTimer -= frame;
                    walkIndex++;
                }
            }
            else
            {
                frameTimer = 0f;
                walkIndex = 0;
            }

            Apply();
        }

        void Apply()
        {
            if (sprites == null || body == null) return;

            Sprite[] frames = Facing switch
            {
                Facing.Back => sprites.back,
                Facing.Left or Facing.Right => sprites.side,
                _ => sprites.front
            };
            if (frames == null || frames.Length == 0) frames = sprites.front;
            if (frames == null || frames.Length == 0) return;

            Sprite s;
            if (IsMoving && frames.Length > 1) s = frames[1 + walkIndex % (frames.Length - 1)];
            else s = frames[0];

            body.sprite = s;
            bool isSide = Facing == Facing.Left || Facing == Facing.Right;
            body.flipX = isSide && (Facing == Facing.Right) == sprites.sideFacesLeft;
        }
    }
}
