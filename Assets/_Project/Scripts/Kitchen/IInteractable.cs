using RamenTycoon.Player;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 플레이어가 상호작용할 수 있는 모든 것(화구, 개수대, 테이블 등)의 공통 인터페이스.
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract(PlayerHand hand);
        void Interact(PlayerHand hand);
        void SetHighlighted(bool on);
    }
}
