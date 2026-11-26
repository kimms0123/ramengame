using System.Collections.Generic;
using RamenTycoon.Data;

namespace RamenTycoon.Logic
{
    /// <summary>
    /// 주문 생성기. 일차가 올라갈수록 고명 종류 수와 개수가 늘어난다 (규칙은 DayConfig > 일차별 설정).
    /// 순수 C# → 시뮬레이터/테스트에서 그대로 사용 가능.
    /// </summary>
    public class OrderGenerator
    {
        readonly System.Random rng;

        public OrderGenerator(int seed) => rng = new System.Random(seed);

        /// <param name="minKinds">고명 종류 수 최소</param>
        /// <param name="maxKinds">고명 종류 수 최대</param>
        /// <param name="maxCount">같은 고명 최대 개수</param>
        /// <param name="extraChance">고명마다 2개 이상이 될 확률</param>
        public Order Generate(IngredientData tonkotsu, IngredientData spicy, IReadOnlyList<IngredientData> toppings,
            float spicyChance, int minKinds, int maxKinds, int maxCount, float extraChance)
        {
            bool isSpicy = spicy != null && rng.NextDouble() < spicyChance;
            var broth = isSpicy ? spicy : tonkotsu;

            // 쓸 수 있는 고명
            var pool = new List<IngredientData>();
            if (toppings != null)
                foreach (var t in toppings)
                    if (t != null) pool.Add(t);

            var dict = new Dictionary<IngredientData, int>();
            if (pool.Count > 0)
            {
                int lo = System.Math.Clamp(minKinds, 1, pool.Count);
                int hi = System.Math.Clamp(maxKinds, lo, pool.Count);
                int kinds = rng.Next(lo, hi + 1);

                // 섞어서 앞에서부터 kinds개
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }

                for (int i = 0; i < kinds; i++)
                {
                    int count = 1;
                    if (maxCount > 1 && rng.NextDouble() < extraChance) count = rng.Next(2, maxCount + 1);
                    dict[pool[i]] = count;
                }
            }

            return new Order(broth, dict);
        }
    }
}