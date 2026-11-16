using System.Collections.Generic;
using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    public enum BowlState
    {
        Clean, // 깨끗한 빈 그릇
        Ramen, // 육수+면이 담긴 상태 (고명 데코 전/후)
        Dirty  // 손님이 먹고 남긴 그릇
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Bowl : HoldableItem
    {
        static readonly Color CleanColor = new(0.95f, 0.95f, 0.92f);
        static readonly Color DirtyColor = new(0.45f, 0.38f, 0.32f);

        SpriteRenderer sr;
        readonly List<IngredientData> toppings = new();

        public BowlState State { get; private set; } = BowlState.Clean;
        public IngredientData Broth { get; private set; }
        public IReadOnlyList<IngredientData> Toppings => toppings;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            Refresh();
        }

        public void FillRamen(IngredientData broth)
        {
            Broth = broth;
            toppings.Clear();
            State = BowlState.Ramen;
            Refresh();
        }

        public void AddTopping(IngredientData topping)
        {
            if (State != BowlState.Ramen) return;
            toppings.Add(topping);
        }

        public void MarkDirty()
        {
            Broth = null;
            toppings.Clear();
            State = BowlState.Dirty;
            Refresh();
        }

        public void Wash()
        {
            State = BowlState.Clean;
            Refresh();
        }

        void Refresh()
        {
            if (sr == null) return;
            sr.color = State switch
            {
                BowlState.Ramen => Broth != null ? Broth.placeholderColor : Color.yellow,
                BowlState.Dirty => DirtyColor,
                _ => CleanColor
            };
            name = $"Bowl_{State}";
        }

        public override string DebugName =>
            State == BowlState.Ramen && Broth != null ? $"라멘({Broth.displayName})" : $"그릇({State})";
    }
}
