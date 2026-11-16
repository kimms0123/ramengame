using UnityEngine;

namespace RamenTycoon.Player
{
    /// <summary>
    /// 플레이어 외형 연결용. Player 오브젝트에 붙인다.
    /// - 이동 중이면 Animator의 Bool 파라미터(movingParam)를 켠다.
    /// - 이동 속도를 Float 파라미터(speedParam)로도 넘긴다.
    /// - 좌우 이동 방향에 맞춰 스프라이트를 뒤집는다 (flipX).
    /// 파라미터가 Animator Controller에 없으면 무시한다(에러 없음).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Header("Animator 파라미터 이름")]
        [SerializeField] string movingParam = "IsMoving";
        [SerializeField] string speedParam = "Speed";

        [Header("스프라이트 기본 방향")]
        [Tooltip("원본 스프라이트가 오른쪽을 보고 있으면 체크")]
        [SerializeField] bool spriteFacesRight = true;

        Rigidbody2D rb;
        bool hasMoving, hasSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            CacheParams();
        }

        void CacheParams()
        {
            hasMoving = hasSpeed = false;
            if (animator == null || animator.runtimeAnimatorController == null) return;

            foreach (var p in animator.parameters)
            {
                if (p.name == movingParam && p.type == AnimatorControllerParameterType.Bool) hasMoving = true;
                if (p.name == speedParam && p.type == AnimatorControllerParameterType.Float) hasSpeed = true;
            }
        }

        void Update()
        {
            Vector2 v = rb.linearVelocity;
            bool moving = v.sqrMagnitude > 0.01f;

            if (animator != null)
            {
                if (hasMoving) animator.SetBool(movingParam, moving);
                if (hasSpeed) animator.SetFloat(speedParam, v.magnitude);
            }

            if (spriteRenderer != null && Mathf.Abs(v.x) > 0.05f)
            {
                bool goingRight = v.x > 0f;
                spriteRenderer.flipX = spriteFacesRight ? !goingRight : goingRight;
            }
        }
    }
}
