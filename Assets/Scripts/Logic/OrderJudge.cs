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
        /// 0~1 정확도. 육수가 틀리면 0.
        /// 정확도 = 1 - (빠진 고명 수 + 초과/잘못 넣은 고명 수) / 필수 고명 수
        /// </summary>
        public static float Accuracy(Order order, IngredientData servedBroth, IReadOnlyList<IngredientData> servedToppings)
        {
            if (servedBroth != order.Broth) return 0f;

            var served = new Dictionary<IngredientData, int>();
            foreach (var t in servedToppings)
            {
                served.TryGetValue(t, out int n);
                served[t] = n + 1;
            }

            int errors = 0;
            foreach (var kv in order.Toppings)
            {
                served.TryGetValue(kv.Key, out int got);
                errors += System.Math.Abs(kv.Value - got);
                served.Remove(kv.Key);
            }
            foreach (var kv in served) errors += kv.Value; // 주문에 없는 고명

            int required = System.Math.Max(1, order.RequiredToppingCount);
            float acc = 1f - (float)errors / required;
            return acc < 0f ? 0f : acc;
        }

        /// <summary>
        /// 손님 1명 점수(0~100). 가중치는 밸런싱하면서 조정한다.
        /// </summary>
        public static float CustomerScore(float waitSeconds, float patienceSeconds, float accuracy, float plating)
        {
            float wait = patienceSeconds <= 0f ? 0f : 1f - waitSeconds / patienceSeconds;
            if (wait < 0f) wait = 0f;
            if (wait > 1f) wait = 1f;
            if (accuracy <= 0f) return 0f;
            return 100f * (0.5f * wait + 0.4f * accuracy + 0.1f * plating);
        }
    }
}
