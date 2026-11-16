using RamenTycoon.Core;
using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class IngredientItem : HoldableItem
    {
        public const float WorldWidth = 0.9f;

        public IngredientData Data { get; private set; }

        public void Init(IngredientData data)
        {
            Data = data;
            name = $"Item_{data.displayName}";

            var sr = GetComponent<SpriteRenderer>();
            if (data.icon != null)
            {
                sr.sprite = data.icon;
                sr.color = Color.white;
            }
            else
            {
                sr.color = data.placeholderColor;
            }
            SpriteFit.FitWidth(sr, WorldWidth);
        }

        public override string DebugName => Data != null ? Data.displayName : name;
    }
}