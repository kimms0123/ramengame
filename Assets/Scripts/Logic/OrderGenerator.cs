using System.Collections.Generic;
using RamenTycoon.Data;

namespace RamenTycoon.Logic
{
    /// <summary>
    /// 조합형 주문 생성기. 육수별 기본 고명 세트 + 확률적으로 변형(추가/빼기/많이).
    /// 순수 C# → 시뮬레이터/테스트에서 그대로 사용 가능.
    /// </summary>
    public class OrderGenerator
    {
        readonly System.Random rng;

        // 육수별 기본 고명 (고명 id 기준)
        static readonly string[] TonkotsuBase = { "chashu", "nori", "scallion" };
        static readonly string[] SpicyBase = { "chashu", "scallion", "egg" };

        public OrderGenerator(int seed) => rng = new System.Random(seed);

        public Order Generate(IngredientData tonkotsu, IngredientData spicy, IReadOnlyList<IngredientData> toppings,
            float spicyChance, float variationChance)
        {
            bool isSpicy = spicy != null && rng.NextDouble() < spicyChance;
            var broth = isSpicy ? spicy : tonkotsu;

            var dict = new Dictionary<IngredientData, int>();
            foreach (var id in isSpicy ? SpicyBase : TonkotsuBase)
            {
                var t = Find(toppings, id);
                if (t != null) dict[t] = 1;
            }

            if (rng.NextDouble() < variationChance) ApplyVariation(dict, toppings);

            return new Order(broth, dict);
        }

        void ApplyVariation(Dictionary<IngredientData, int> dict, IReadOnlyList<IngredientData> toppings)
        {
            var keys = new List<IngredientData>(dict.Keys);
            switch (rng.Next(3))
            {
                case 0: // 많이 ("차슈 많이")
                    if (keys.Count > 0) dict[keys[rng.Next(keys.Count)]]++;
                    break;

                case 1: // 빼기 ("파 빼주세요") — 고명이 하나는 남게
                    if (keys.Count > 1) dict.Remove(keys[rng.Next(keys.Count)]);
                    break;

                default: // 추가 ("계란 추가")
                    var candidates = new List<IngredientData>();
                    if (toppings != null)
                    {
                        foreach (var t in toppings)
                        {
                            if (t != null && !dict.ContainsKey(t)) candidates.Add(t);
                        }
                    }
                    if (candidates.Count > 0) dict[candidates[rng.Next(candidates.Count)]] = 1;
                    else if (keys.Count > 0) dict[keys[rng.Next(keys.Count)]]++;
                    break;
            }
        }

        static IngredientData Find(IReadOnlyList<IngredientData> list, string id)
        {
            if (list == null) return null;
            foreach (var t in list)
            {
                if (t != null && t.id == id) return t;
            }
            return null;
        }
    }
}
