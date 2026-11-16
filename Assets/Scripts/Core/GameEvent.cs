using System;
using RamenTycoon.Kitchen;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 시스템 간 연결용 이벤트 허브. 시스템끼리 직접 참조하지 않고 여기로만 통신한다.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>라멘이 테이블에 서빙됨</summary>
        public static event Action<Bowl> RamenServed;
        public static event Action<GameState> StateChanged;

        /// <summary>테이블 번호, 손님 점수(0~100), 받은 돈</summary>
        public static event Action<int, float, int> CustomerServed;

        /// <summary>기다리다 지쳐서 나간 손님 (테이블 번호)</summary>
        public static event Action<int> CustomerLost;

        public static void RaiseRamenServed(Bowl bowl) => RamenServed?.Invoke(bowl);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);
        public static void RaiseCustomerServed(int table, float score, int pay) => CustomerServed?.Invoke(table, score, pay);
        public static void RaiseCustomerLost(int table) => CustomerLost?.Invoke(table);

        // Enter Play Mode 옵션에서 Domain Reload를 끈 경우를 대비해 구독을 초기화한다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            RamenServed = null;
            StateChanged = null;
            CustomerServed = null;
            CustomerLost = null;
        }
    }
}