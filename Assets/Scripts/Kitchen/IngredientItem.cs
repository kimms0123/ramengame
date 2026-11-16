using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class IngredientItem : HoldableItem
    {
        public IngredientData Data { get; private set; }

        public void Init(IngredientData data)
        {
            Data = data;
            name = $"Item_{data.displayName}";
        }

        public override string DebugName => Data != null ? Data.displayName : name;
    }
}
