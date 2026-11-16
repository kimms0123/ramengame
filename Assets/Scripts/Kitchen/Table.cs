using RamenTycoon.Core;
using RamenTycoon.Customers;
using RamenTycoon.Logic;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 홀 테이블. 손님이 앉으면 주문이 빌지에 올라가고, 플레이어가 라멘을 직접 들고 와서 서빙한다.
    /// 상태: Empty → Reserved(손님 걸어오는 중) → WaitingFood → Eating → Dirty → (빈손으로 그릇 수거) → Empty
    /// </summary>
    public class Table : Station
    {
        public enum TableState { Empty, Reserved, WaitingFood, Eating, Dirty }

        [SerializeField] int tableNo = 1;
        [Tooltip("손님이 서 있는(앉는) 위치")]
        [SerializeField] Transform seatPoint;

        Customer customer;
        Bowl bowl;
        float timer;
        float patience = 60f;

        public int TableNo => tableNo;
        public TableState State { get; private set; } = TableState.Empty;
        public bool IsFree => State == TableState.Empty && isActiveAndEnabled;
        public Vector3 SeatPosition => seatPoint != null ? seatPoint.position : transform.position + Vector3.down;
        public Customer Customer => customer;

        /// <summary>남은 인내심 0~1 (WaitingFood일 때만 의미 있음)</summary>
        public float PatienceRatio => State == TableState.WaitingFood ? Mathf.Clamp01(1f - timer / patience) : 1f;

        // ───────── 손님 흐름 ─────────

        public void Reserve(Customer c)
        {
            customer = c;
            State = TableState.Reserved;
        }

        /// <summary>손님이 자리에 도착 → 주문</summary>
        public void OnCustomerArrived(Order order, float patienceSeconds)
        {
            timer = 0f;
            patience = Mathf.Max(5f, patienceSeconds);
            State = TableState.WaitingFood;
            if (OrderBoard.Instance != null) OrderBoard.Instance.Add(tableNo, order);
        }

        /// <summary>하루 마감 시 강제 정리</summary>
        public void ForceClear()
        {
            if (customer != null) customer.Leave(false);
            customer = null;
            if (bowl != null) Destroy(bowl.gameObject);
            bowl = null;
            if (OrderBoard.Instance != null) OrderBoard.Instance.Remove(tableNo);
            State = TableState.Empty;
        }

        // ───────── 상호작용 ─────────

        public override bool CanInteract(PlayerHand hand)
        {
            switch (State)
            {
                case TableState.WaitingFood:
                    return hand.Held is Bowl served && served.IsRamen;
                case TableState.Dirty:
                    return hand.IsEmpty && bowl != null;
                default:
                    return false;
            }
        }

        public override string GetHint(PlayerHand hand)
        {
            switch (State)
            {
                case TableState.Empty: return "빈 테이블이에요";
                case TableState.Reserved: return "손님이 자리로 오는 중이에요";
                case TableState.Eating: return "손님이 식사 중이에요";
                case TableState.Dirty: return "빈손으로 와서 그릇을 치워요";
                default:
                    if (hand.Held is Bowl b && !b.IsRamen) return "육수와 면이 모두 담긴 라멘을 내야 해요";
                    return "라멘을 들고 와요";
            }
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (State == TableState.WaitingFood) Serve(hand);
            else PickUpDirty(hand);
        }

        void Serve(PlayerHand hand)
        {
            var served = (Bowl)hand.Release();
            var board = OrderBoard.Instance;
            var ticket = board != null ? board.Get(tableNo) : null;

            // 다른 테이블용으로 만든 그릇을 여기 냈으면, 그 테이블 빌지의 취소선을 되돌린다
            if (board != null && served.AssignedTable > 0 && served.AssignedTable != tableNo) board.Unassign(served);

            var cfg = GameDatabase.Days;
            float acc = ticket != null ? OrderJudge.Accuracy(ticket.Order, served.Broth, served.HasNoodle, served.Toppings) : 0f;
            float waitScore = OrderJudge.WaitScore(timer, patience);
            float score = OrderJudge.CustomerScore(timer, patience, acc);
            int price = ticket != null ? ticket.Order.Price : 0;
            int pay = OrderJudge.Payment(price, acc, waitScore, cfg != null ? cfg.maxTipRate : 0.2f);

            if (Economy.Instance != null) Economy.Instance.RecordServe(pay, score, acc < 0.999f);
            if (board != null) board.Remove(tableNo);

            served.AssignedTable = 0;
            served.AttachTo(itemAnchor);
            bowl = served;
            timer = 0f;
            State = TableState.Eating;

            if (customer != null) customer.ShowMood(score);
            GameEvents.RaiseRamenServed(served);
            GameEvents.RaiseCustomerServed(tableNo, score, pay);
            Debug.Log($"[Table {tableNo}] 서빙: 정확도 {acc:P0}, 점수 {score:0}, +{pay}C");
        }

        void PickUpDirty(PlayerHand hand)
        {
            hand.Hold(bowl);
            bowl = null;
            State = TableState.Empty;
        }

        // ───────── 시간 ─────────

        void Update()
        {
            switch (State)
            {
                case TableState.Reserved:
                    if (customer == null) State = TableState.Empty; // 손님이 사라짐
                    break;

                case TableState.WaitingFood:
                    timer += GameClock.DeltaTime;
                    if (timer >= patience) GiveUp();
                    break;

                case TableState.Eating:
                    timer += GameClock.DeltaTime;
                    float eat = GameDatabase.Days != null ? GameDatabase.Days.eatSeconds : 8f;
                    if (timer >= eat) FinishEating();
                    break;
            }
        }

        void GiveUp()
        {
            if (OrderBoard.Instance != null) OrderBoard.Instance.Remove(tableNo);
            if (Economy.Instance != null) Economy.Instance.RecordLost();
            if (customer != null) customer.Leave(true);
            customer = null;
            State = TableState.Empty;
            GameEvents.RaiseCustomerLost(tableNo);
            Debug.Log($"[Table {tableNo}] 손님이 기다리다 나갔어요");
        }

        void FinishEating()
        {
            if (bowl != null) bowl.MarkDirty();
            if (customer != null) customer.Leave(false);
            customer = null;
            State = TableState.Dirty;
        }
    }
}