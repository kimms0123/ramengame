using UnityEngine;
using UnityEngine.InputSystem;

namespace RamenTycoon.Player
{
    /// <summary>
    /// 탑뷰 이동. WASD/방향키/게임패드 왼쪽 스틱.
    /// Facing은 마지막으로 이동한 방향이며, 상호작용 대상 선택에 쓰인다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4.5f;

        Rigidbody2D rb;
        InputAction moveAction;
        Vector2 moveInput;

        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool InputEnabled { get; set; } = true;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }

        void OnEnable() => moveAction.Enable();
        void OnDisable() => moveAction.Disable();
        void OnDestroy() => moveAction.Dispose();

        void Update()
        {
            moveInput = InputEnabled ? Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f) : Vector2.zero;
            if (moveInput.sqrMagnitude > 0.01f) Facing = moveInput.normalized;
        }

        void FixedUpdate()
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }
}
