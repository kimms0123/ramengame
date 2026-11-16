using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 육수 통 (돈코츠 / 매운). 그릇을 들고 와서 상호작용하면 육수를 붓는다.
    /// 부을 때 재료비가 차감된다.
    /// </summary>
    public class BrothUrn : Station
    {
        [SerializeField] IngredientData broth;

        public IngredientData Broth => broth;

        public override bool CanInteract(PlayerHand hand)
        {
            return broth != null && hand.Held is Bowl bowl && bowl.CanTakeBroth;
        }

        public override string GetHint(PlayerHand hand)
        {
            if (hand.IsEmpty) return "싱크대에서 그릇을 먼저 가져와요";
            if (hand.Held is not Bowl bowl) return "그릇을 들고 와야 육수를 담을 수 있어요";
            if (bowl.IsDirty) return "더러운 그릇이에요. 싱크대에 넣어요";
            if (bowl.Broth != null) return "이미 육수가 담겨 있어요";
            return null;
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;
            ((Bowl)hand.Held).AddBroth(broth);
            if (Economy.Instance != null) Economy.Instance.SpendIngredient(broth.cost);
        }
    }
}