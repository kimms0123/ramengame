using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 재료 통. 빈손으로 상호작용하면 재료 하나를 꺼낸다. 같은 재료를 들고 오면 다시 넣는다.
    /// </summary>
    public class IngredientCrate : Station
    {
        [SerializeField] IngredientData ingredient;

        public IngredientData Ingredient => ingredient;

        public override bool CanInteract(PlayerHand hand)
        {
            if (ingredient == null) return false;
            if (hand.IsEmpty) return true;
            return hand.Held is IngredientItem item && item.Data == ingredient;
        }

        public override void Interact(PlayerHand hand)
        {
            if (hand.IsEmpty)
            {
                hand.Hold(ItemFactory.Instance.CreateIngredient(ingredient));
            }
            else if (hand.Held is IngredientItem item && item.Data == ingredient)
            {
                Destroy(hand.Release().gameObject);
            }
        }
    }
}
