using RamenTycoon.Player;
using RamenTycoon.UI;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 고명 작업대. 육수+면이 담긴 그릇을 들고 상호작용하면 고명 데코 화면(1인칭)이 열린다.
    /// 그릇은 계속 플레이어 손에 있고, 화면에서 완성하면 고명이 반영된다.
    /// </summary>
    public class ToppingStation : Station
    {
        public override bool CanInteract(PlayerHand hand)
        {
            return ToppingScreen.Instance != null && hand.Held is Bowl bowl && bowl.IsRamen;
        }

        public override string GetHint(PlayerHand hand)
        {
            if (hand.Held is Bowl bowl)
            {
                if (bowl.IsDirty) return "더러운 그릇이에요";
                if (bowl.Broth == null) return "육수를 먼저 담아요";
                if (!bowl.HasNoodle) return "화구에서 면을 먼저 담아요";
            }
            return "육수와 면이 담긴 그릇을 들고 와요";
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;
            ToppingScreen.Instance.Open((Bowl)hand.Held);
        }
    }
}