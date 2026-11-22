using System;
using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 그릇. 육수/면은 순서 상관없이 담을 수 있고, 둘 다 들어가면 라멘(IsRamen)이 된다.
    /// 고명은 고명 데코 화면에서만 넣는다. 손님이 먹고 나면 더러운 그릇(IsDirty)이 된다.
    ///
    /// 이미지: GameArtConfig의 bowl* 칸. 비어 있으면 네모+색으로 표시.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Bowl : HoldableItem
    {
        public const float WorldWidth = 1.1f; // 시안 기준 (플레이어 키 약 3.4유닛)

        static readonly Color CleanColor = new(0.95f, 0.95f, 0.92f);
        static readonly Color DirtyColor = new(0.45f, 0.38f, 0.32f);
        static readonly Color NoodleColor = new(1f, 0.9f, 0.5f);

        SpriteRenderer sr;
        Sprite placeholder;
        readonly Dictionary<IngredientData, int> toppings = new();

        public IngredientData Broth { get; private set; }
        public bool HasNoodle { get; private set; }
        public bool IsDirty { get; private set; }
        public bool Decorated { get; private set; }

        /// <summary>빌지에서 배정된 테이블 번호 (0 = 미배정)</summary>
        public int AssignedTable { get; set; }

        public IReadOnlyDictionary<IngredientData, int> Toppings => toppings;

        public bool IsEmpty => !IsDirty && Broth == null && !HasNoodle;
        public bool IsRamen => !IsDirty && Broth != null && HasNoodle;
        public bool CanTakeBroth => !IsDirty && Broth == null;
        public bool CanTakeNoodle => !IsDirty && !HasNoodle;

        /// <summary>내용물이 바뀔 때마다 (말풍선 갱신용)</summary>
        public event Action Changed;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            placeholder = sr.sprite;
            Refresh();
        }

        public void AddBroth(IngredientData broth)
        {
            if (!CanTakeBroth) return;
            Broth = broth;
            Refresh();
        }

        public void AddNoodle()
        {
            if (!CanTakeNoodle) return;
            HasNoodle = true;
            Refresh();
        }

        public void SetToppings(IReadOnlyDictionary<IngredientData, int> newToppings)
        {
            if (!IsRamen) return;
            toppings.Clear();
            if (newToppings != null)
            {
                foreach (var kv in newToppings)
                {
                    if (kv.Value > 0) toppings[kv.Key] = kv.Value;
                }
            }
            Decorated = true;
            Refresh();
        }

        public void MarkDirty()
        {
            Broth = null;
            HasNoodle = false;
            Decorated = false;
            AssignedTable = 0;
            toppings.Clear();
            IsDirty = true;
            Refresh();
        }

        void Refresh()
        {
            if (sr == null) return;

            var art = GameDatabase.Art;
            var db = GameDatabase.Instance;
            bool spicy = db != null && Broth != null && Broth == db.spicy;

            Sprite sprite = null;
            if (art != null)
            {
                if (IsDirty) sprite = art.bowlDirty;
                else if (IsRamen) sprite = spicy ? art.bowlRamenSpicy : art.bowlRamenTonkotsu;
                else if (Broth != null) sprite = spicy ? art.bowlBrothSpicy : art.bowlBrothTonkotsu;
                else sprite = art.bowlClean;
            }

            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
            }
            else if (art != null && art.bowlClean != null)
            {
                // 전용 그림이 없는 상태(육수만 / 면만 / 더러움)는 빈 그릇 그림에 색을 입혀 구분
                sr.sprite = art.bowlClean;
                sr.color = IsDirty ? new Color(0.62f, 0.55f, 0.48f)
                    : Broth != null ? Color.Lerp(Color.white, Broth.placeholderColor, 0.45f)
                    : HasNoodle ? Color.Lerp(Color.white, NoodleColor, 0.45f)
                    : Color.white;
            }
            else
            {
                sr.sprite = placeholder;
                sr.color = IsDirty ? DirtyColor
                    : Broth != null ? (HasNoodle ? Color.Lerp(Broth.placeholderColor, NoodleColor, 0.35f) : Broth.placeholderColor)
                    : HasNoodle ? NoodleColor
                    : CleanColor;
            }

            SpriteFit.FitWidth(sr, WorldWidth);
            name = IsDirty ? "Bowl_Dirty" : IsRamen ? "Bowl_Ramen" : "Bowl";
            Changed?.Invoke();
        }

        public override string DebugName
        {
            get
            {
                if (IsDirty) return "더러운 그릇";
                if (IsEmpty) return "빈 그릇";
                string b = Broth != null ? Broth.displayName : "육수 없음";
                return HasNoodle ? $"라멘({b})" : b;
            }
        }
    }
}
