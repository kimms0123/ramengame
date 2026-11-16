using System;
using RamenTycoon.Kitchen;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 시스템 간 연결용 이벤트 허브. 시스템끼리 직접 참조하지 않고 여기로만 통신한다.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<Bowl> RamenServed;
        public static event Action<GameState> StateChanged;

        public static void RaiseRamenServed(Bowl bowl) => RamenServed?.Invoke(bowl);
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);

        // Enter Play Mode 옵션에서 Domain Reload를 끈 경우를 대비해 구독을 초기화한다.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            RamenServed = null;
            StateChanged = null;
        }
    }
}