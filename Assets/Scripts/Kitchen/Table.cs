using RamenTycoon.Core;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 홀 테이블. 플레이어가 라멘을 직접 들고 와서 내려놓으면 서빙 완료로 처리한다.
    /// 상태: Empty → (라멘 내려놓음) → Eating → (식사 끝) → Dirty → (빈손으로 수거) → Empty
    ///
    /// M1에서는 손님 없이 테이블이 대신 "식사"한다.
    /// M2에서는 손님이 착석해야 Eating으로 바뀌고, 주문 판정은 RamenServed 이벤트에서 처리한다.
    /// </summary>
    public class Table : Station
    {
        public enum TableState { Empty, Eating, Dirty }

        [SerializeField] float eatSeconds = 6f;

        Bowl bowl;
        float timer;

        public TableState State { get; private set; } = TableState.Empty;

        public override bool CanInteract(PlayerHand hand)
        {
            switch (State)
            {
                case TableState.Empty:
                    return hand.Held is Bowl served && served.State == BowlState.Ramen;
                case TableState.Dirty:
                    return hand.IsEmpty;
                default:
                    return false;
            }
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (State == TableState.Empty)
            {
                bowl = (Bowl)hand.Release();
                bowl.AttachTo(itemAnchor);
                timer = 0f;
                State = TableState.Eating;
                GameEvents.RaiseRamenServed(bowl);
            }
            else
            {
                hand.Hold(bowl);
                bowl = null;
                State = TableState.Empty;
            }
        }

        void Update()
        {
            if (State != TableState.Eating) return;

            timer += GameClock.DeltaTime;
            if (timer >= eatSeconds)
            {
                bowl.MarkDirty();
                State = TableState.Dirty;
            }
        }
    }
}
