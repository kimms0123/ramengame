using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 재료 통 (면 상자). 빈손으로 상호작용하면 재료 하나를 꺼낸다. 같은 재료를 들고 오면 다시 넣는다.
    /// Ingredient 칸이 비어 있으면 GameDatabase의 면을 자동으로 사용한다.
    /// </summary>
    public class IngredientCrate : Station
    {
        [SerializeField] IngredientData ingredient;

        public IngredientData Ingredient
        {
            get
            {
                EnsureIngredient();
                return ingredient;
            }
        }

        void Start() => EnsureIngredient();

        void EnsureIngredient()
        {
            if (ingredient != null) return;
            var db = GameDatabase.Instance;
            if (db == null || db.noodle == null) return;
            ingredient = db.noodle;
            Debug.LogWarning($"[IngredientCrate] {name}의 Ingredient 칸이 비어 있어서 '{ingredient.displayName}'을(를) 자동으로 연결했어요.");
        }

        public override bool CanInteract(PlayerHand hand)
        {
            EnsureIngredient();
            if (ingredient == null) return false;
            if (hand.IsEmpty) return true;
            return hand.Held is IngredientItem item && item.Data == ingredient;
        }

        public override string GetHint(PlayerHand hand)
        {
            EnsureIngredient();
            if (ingredient == null) return "재료가 연결되지 않았어요 (GameDatabase의 Noodle 칸 확인)";
            if (ingredient.category == IngredientCategory.Noodle && hand.Held is Bowl)
                return "면은 화구에 넣어 삶은 뒤 그릇에 담아요";
            return "손이 비어 있어야 꺼낼 수 있어요";
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

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
