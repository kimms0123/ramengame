using RamenTycoon.Player;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 물건을 하나 올려둘 수 있는 일반 조리대.
    /// </summary>
    public class Counter : Station
    {
        HoldableItem placed;

        public override bool CanInteract(PlayerHand hand)
        {
            if (hand.IsEmpty) return placed != null;
            return placed == null;
        }

        public override void Interact(PlayerHand hand)
        {
            if (hand.IsEmpty && placed != null)
            {
                hand.Hold(placed);
                placed = null;
            }
            else if (!hand.IsEmpty && placed == null)
            {
                placed = hand.Release();
                placed.AttachTo(itemAnchor);
            }
        }
    }
}
