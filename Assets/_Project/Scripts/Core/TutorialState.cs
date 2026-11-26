using System.Collections.Generic;
using RamenTycoon.Data;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 튜토리얼이 게임에 거는 제한들. TutorialGuide가 켜고 끈다.
    /// - FreezeShop: 가게 시간(영업 시계·손님 인내심·새 손님)을 멈춘다. 화구(면 삶기)는 계속 움직인다.
    /// - AllowInteract: 지금 단계에서 E로 쓸 수 있는 설비만 true. null이면 제한 없음.
    /// - RequiredToppings: 토핑 화면에서 주문서에 있는 토핑만, 정해진 개수만 올릴 수 있게 한다. null이면 제한 없음.
    /// </summary>
    public static class TutorialState
    {
        public static bool FreezeShop;
        public static System.Func<object, bool> AllowInteract;
        public static string BlockedHint = "화살표가 가리키는 곳으로 가요!";
        public static IReadOnlyDictionary<IngredientData, int> RequiredToppings;

        public static bool Active => AllowInteract != null || RequiredToppings != null;

        public static bool IsAllowed(object target) => AllowInteract == null || AllowInteract(target);

        /// <summary>올린 토핑이 주문서와 정확히 같은지</summary>
        public static bool ToppingsMatch(IReadOnlyDictionary<IngredientData, int> placed)
        {
            if (RequiredToppings == null) return true;
            foreach (var kv in RequiredToppings)
            {
                int n = 0;
                if (placed != null) placed.TryGetValue(kv.Key, out n);
                if (n != kv.Value) return false;
            }
            if (placed != null)
                foreach (var kv in placed)
                    if (kv.Value > 0 && !RequiredToppings.ContainsKey(kv.Key)) return false;
            return true;
        }

        public static void Clear()
        {
            FreezeShop = false;
            AllowInteract = null;
            RequiredToppings = null;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();
    }
}