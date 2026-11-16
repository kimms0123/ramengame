using System;
using System.Collections.Generic;
using RamenTycoon.Kitchen;
using RamenTycoon.Logic;
using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 빌지(주문서). 지금 받은 모든 테이블의 주문을 들고 있다.
    /// 고명까지 완성된 그릇이 나오면 가장 잘 맞는 주문에 자동으로 배정하고 취소선(Made)을 긋는다.
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        public static OrderBoard Instance { get; private set; }

        readonly List<OrderTicket> tickets = new();

        public IReadOnlyList<OrderTicket> Tickets => tickets;
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public OrderTicket Add(int tableNo, Order order)
        {
            Remove(tableNo);
            var t = new OrderTicket { TableNo = tableNo, Order = order, CreatedAt = Time.time };
            tickets.Add(t);
            Changed?.Invoke();
            return t;
        }

        public void Remove(int tableNo)
        {
            if (tickets.RemoveAll(t => t.TableNo == tableNo) > 0) Changed?.Invoke();
        }

        public OrderTicket Get(int tableNo) => tickets.Find(t => t.TableNo == tableNo);

        public void Clear()
        {
            tickets.Clear();
            Changed?.Invoke();
        }

        /// <summary>
        /// 완성된 그릇을 가장 잘 맞는 주문에 배정한다. (같은 육수 중 정확도 최고, 동점이면 먼저 들어온 주문)
        /// 배정되면 bowl.AssignedTable이 정해지고 빌지에 취소선이 그어진다.
        /// </summary>
        public OrderTicket AssignBestMatch(Bowl bowl)
        {
            if (bowl == null || !bowl.IsRamen) return null;

            // 이미 다른 주문에 배정돼 있었다면 먼저 풀어준다 (고명을 다시 고친 경우)
            Unassign(bowl);

            OrderTicket best = null;
            float bestAcc = -1f;
            foreach (var t in tickets)
            {
                if (t.Made || t.Order.Broth != bowl.Broth) continue;
                float acc = OrderJudge.Accuracy(t.Order, bowl.Broth, bowl.HasNoodle, bowl.Toppings);
                if (acc > bestAcc + 0.0001f || (Mathf.Abs(acc - bestAcc) < 0.0001f && best != null && t.CreatedAt < best.CreatedAt))
                {
                    best = t;
                    bestAcc = acc;
                }
            }

            if (best != null)
            {
                best.Made = true;
                bowl.AssignedTable = best.TableNo;
                Changed?.Invoke();
            }
            return best;
        }

        /// <summary>그릇에 배정된 주문의 취소선을 푼다 (다른 테이블에 냈거나 다시 고칠 때)</summary>
        public void Unassign(Bowl bowl)
        {
            if (bowl == null || bowl.AssignedTable <= 0) return;
            var t = Get(bowl.AssignedTable);
            if (t != null) t.Made = false;
            bowl.AssignedTable = 0;
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
