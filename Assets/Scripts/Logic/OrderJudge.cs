using System.Collections.Generic;
using RamenTycoon.Data;

namespace RamenTycoon.Logic
{
    /// <summary>
    /// 주문 판정 (멀티셋 비교). 순수 함수만 둔다.
    /// </summary>
    public static class OrderJudge
    {
        /// <summary>
        /// 0~1 정확도. 육수가 틀리거나 면이 없으면 0.
        /// 정확도 = 1 - (빠진 고명 수 + 초과/잘못 넣은 고명 수) / 필수 고명 수
        /// </summary>
        public static float Accuracy(Order order, IngredientData servedBroth, bool hasNoodle,
            IReadOnlyDictionary<IngredientData, int> servedToppings)
        {
            if (order == null || servedBroth != order.Broth || !hasNoodle) return 0f;

            int errors = 0;
            foreach (var kv in order.Toppings)
            {
                int got = 0;
                if (servedToppings != null) servedToppings.TryGetValue(kv.Key, out got);
                errors += System.Math.Abs(kv.Value - got);
            }

            if (servedToppings != null)
            {
                foreach (var kv in servedToppings)
                {
                    if (!order.Toppings.ContainsKey(kv.Key)) errors += kv.Value; // 주문에 없는 고명
                }
            }

            int required = System.Math.Max(1, order.RequiredToppingCount);
            float acc = 1f - (float)errors / required;
            return acc < 0f ? 0f : acc;
        }

        /// <summary>대기 점수 0~1 (빨리 낼수록 1)</summary>
        public static float WaitScore(float waitSeconds, float patienceSeconds)
        {
            if (patienceSeconds <= 0f) return 0f;
            float w = 1f - waitSeconds / patienceSeconds;
            return w < 0f ? 0f : (w > 1f ? 1f : w);
        }

        /// <summary>
        /// 손님 1명 점수(0~100). 정확도가 0(육수 틀림)이면 0점.
        /// </summary>
        public static float CustomerScore(float waitSeconds, float patienceSeconds, float accuracy)
        {
            if (accuracy <= 0f) return 0f;
            return 100f * (0.5f * WaitScore(waitSeconds, patienceSeconds) + 0.5f * accuracy);
        }

        /// <summary>
        /// 받는 돈. 육수 틀림 → 0. 고명이 틀리면 정확도만큼 깎이고(최소 절반), 빨리 내면 팁.
        /// </summary>
        public static int Payment(int price, float accuracy, float waitScore, float maxTipRate)
        {
            if (accuracy <= 0f) return 0;
            float basePay = price * (0.5f + 0.5f * accuracy);
            float tip = accuracy >= 0.999f ? price * maxTipRate * waitScore : 0f;
            return (int)System.Math.Round(basePay + tip);
        }
    }
}
