using RamenTycoon.Core;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 서빙 카운터(패스). M1에서는 라멘을 올리면 바로 서빙 완료로 처리한다.
    /// M2에서는 여기서 주문과 매칭해 손님 테이블로 보낸다.
    /// </summary>
    public class ServeCounter : Station
    {
        public override bool CanInteract(PlayerHand hand)
        {
            return hand.Held is Bowl bowl && bowl.State == BowlState.Ramen;
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            var bowl = (Bowl)hand.Release();
            Debug.Log($"[Serve] {bowl.DebugName} 서빙 완료");
            GameEvents.RaiseRamenServed(bowl);
            Destroy(bowl.gameObject);
        }
    }
}
