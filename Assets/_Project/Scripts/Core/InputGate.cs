using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 게임플레이 입력 차단 스위치. 고명 화면이나 정산 패널이 열려 있을 때 true로 둔다.
    /// PlayerController / PlayerInteractor가 이 값을 확인한다.
    /// </summary>
    public static class InputGate
    {
        public static bool GameplayBlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => GameplayBlocked = false;
    }
}
