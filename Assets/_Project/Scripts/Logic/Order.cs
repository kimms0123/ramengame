using System.Collections.Generic;
using System.Text;
using RamenTycoon.Data;

namespace RamenTycoon.Logic
{
    /// <summary>
    /// 주문 1건 (육수 + 고명 개수). 면은 항상 1개 들어간다고 본다.
    /// 순수 C# 클래스 → 에디터 시뮬레이터/테스트에서 그대로 사용 가능.
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

        public int Price => Broth != null ? Broth.menuPrice : 0;

        /// <summary>"돈코츠 · 차슈×2 · 김 · 파" 형태</summary>
        public string Describe(IReadOnlyList<IngredientData> toppingOrder = null)
        {
            var sb = new StringBuilder();
            sb.Append(Broth != null ? Broth.displayName : "?");

            IEnumerable<IngredientData> keys = toppingOrder ?? (IEnumerable<IngredientData>)toppings.Keys;
            foreach (var t in keys)
            {
                if (t == null || !toppings.TryGetValue(t, out int n) || n <= 0) continue;
                sb.Append(" · ").Append(t.displayName);
                if (n > 1) sb.Append('×').Append(n);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 빌지(주문서)의 한 줄. 테이블 번호 + 주문 + 만들었는지 여부.
    /// </summary>
    public class OrderTicket
    {
        public int TableNo;
        public Order Order;

        /// <summary>고명까지 완성된 그릇이 이 주문에 배정됨 → 빌지에 취소선</summary>
        public bool Made;

        public float CreatedAt;
    }
}
