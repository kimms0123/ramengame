using RamenTycoon.Core;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// (사용 안 함) 예전 그레이박스의 서빙 카운터. 지금은 테이블에 직접 서빙한다.
    /// 이전 도구(ReplaceServeCounterWithTables)가 컴파일되도록 남겨 두었다. 그 도구와 함께 지워도 된다.
    /// </summary>
    public class ServeCounter : Station
    {
        public override bool CanInteract(PlayerHand hand)
        {
            return hand.Held is Bowl bowl && bowl.IsRamen;
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;
            var bowl = (Bowl)hand.Release();
            GameEvents.RaiseRamenServed(bowl);
            Destroy(bowl.gameObject);
            Debug.Log("[ServeCounter] 서빙 (사용 안 하는 스테이션)");
        }
    }
}
