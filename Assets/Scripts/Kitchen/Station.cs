using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 주방 스테이션 기반 클래스.
    /// 구조: 루트(콜라이더 + 이 컴포넌트) / Body(스프라이트) / ItemAnchor(물건 놓는 위치)
    /// 루트는 스케일을 1로 유지해야 위에 놓인 물건 크기가 찌그러지지 않는다.
    /// </summary>
    public abstract class Station : MonoBehaviour, IInteractable
    {
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected Transform itemAnchor;

        static readonly Color LockedTint = new(0.35f, 0.35f, 0.38f, 1f);

        Color idleColor;
        Color highlightColor;
        bool colorsReady;

        public bool IsLocked { get; private set; }

        protected virtual void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (itemAnchor == null) itemAnchor = transform;
            PrepareColors();
        }

        void PrepareColors()
        {
            if (colorsReady || bodyRenderer == null) return;
            colorsReady = true;

            Color c = bodyRenderer.color;
            // 스프라이트 원본색(흰색 틴트)이면 밝히는 방식으로는 하이라이트가 안 보이므로,
            // 평소에는 살짝 어둡게 두고 하이라이트일 때 원본색으로 돌린다.
            bool isWhiteTint = c.maxColorComponent > 0.95f;
            idleColor = isWhiteTint ? new Color(0.78f, 0.78f, 0.78f, c.a) : c;
            highlightColor = isWhiteTint ? c : Color.Lerp(c, Color.white, 0.35f);
            bodyRenderer.color = idleColor;
        }

        public abstract bool CanInteract(PlayerHand hand);
        public abstract void Interact(PlayerHand hand);

        /// <summary>상호작용이 안 될 때 머리 위에 띄울 안내 문구 (null이면 표시 안 함)</summary>
        public virtual string GetHint(PlayerHand hand) => null;

        public virtual void SetHighlighted(bool on)
        {
            if (bodyRenderer == null || IsLocked) return;
            bodyRenderer.color = on ? highlightColor : idleColor;
        }

        /// <summary>
        /// 업그레이드 전 잠긴 상태. 보이기는 하지만 어둡고, 상호작용/동작하지 않는다.
        /// </summary>
        public void SetLocked(bool locked)
        {
            PrepareColors();
            IsLocked = locked;
            enabled = !locked;
            if (bodyRenderer != null) bodyRenderer.color = locked ? idleColor * LockedTint : idleColor;
        }
    }
}