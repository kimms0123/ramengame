using System.Collections.Generic;
using RamenTycoon.Data;

namespace RamenTycoon.Logic
{
    /// <summary>
    /// 주문 1건. 순수 C# 클래스 (MonoBehaviour 아님) → 에디터 시뮬레이터/테스트에서 그대로 사용 가능.
    /// </summary>
    public class Order
    {
        public IngredientData Broth { get; }
        public IReadOnlyDictionary<IngredientData, int> Toppings => toppings;

        readonly Dictionary<IngredientData, int> toppings;

        public Order(IngredientData broth, Dictionary<IngredientData, int> toppings)
        {
            Broth = broth;
            this.toppings = toppings ?? new Dictionary<IngredientData, int>();
        }

        public int RequiredToppingCount
        {
            get
            {
                int sum = 0;
                foreach (var kv in toppings) sum += kv.Value;
                return sum;
            }
        }
    }
}
