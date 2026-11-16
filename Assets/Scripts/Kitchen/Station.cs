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

        Color baseColor;

        protected virtual void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (itemAnchor == null) itemAnchor = transform;
            if (bodyRenderer != null) baseColor = bodyRenderer.color;
        }

        public abstract bool CanInteract(PlayerHand hand);
        public abstract void Interact(PlayerHand hand);

        public virtual void SetHighlighted(bool on)
        {
            if (bodyRenderer == null) return;
            bodyRenderer.color = on ? Color.Lerp(baseColor, Color.white, 0.35f) : baseColor;
        }
    }
}
