using System;
using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 돈과 만족도, 하루 통계. UI는 Changed 이벤트를 듣고 갱신한다.
    /// </summary>
    public class Economy : MonoBehaviour
    {
        public static Economy Instance { get; private set; }

        public int Coins { get; private set; }
        public float Satisfaction { get; private set; }

        // 오늘 통계 (정산 패널용)
        public int DayRevenue { get; private set; }
        public int DayIngredientCost { get; private set; }
        public int DayServed { get; private set; }
        public int DayLost { get; private set; }
        public int DayMistakes { get; private set; }
        public int DayUpgradeCost { get; private set; }

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

        void Start()
        {
            var save = SaveSystem.Pending;
            if (save != null)
            {
                LoadState(save.coins, save.satisfaction);
                return;
            }
            var cfg = GameDatabase.Days;
            Coins = cfg != null ? cfg.startCoins : 60;
            Satisfaction = cfg != null ? cfg.startSatisfaction : 70f;
            Changed?.Invoke();
        }

        /// <summary>이어하기: 저장된 돈·만족도로 맞춤</summary>
        public void LoadState(int coins, float satisfaction)
        {
            Coins = coins;
            Satisfaction = Mathf.Clamp(satisfaction, 0f, 100f);
            Changed?.Invoke();
        }

        public void ResetDayStats()
        {
            DayRevenue = DayIngredientCost = DayServed = DayLost = DayMistakes = DayUpgradeCost = 0;
            Changed?.Invoke();
        }

        public void SpendIngredient(int cost)
        {
            if (cost <= 0) return;
            Coins -= cost;
            DayIngredientCost += cost;
            Changed?.Invoke();
        }

        /// <summary>서빙 결과 반영</summary>
        public void RecordServe(int pay, float score, bool mistake)
        {
            Coins += pay;
            DayRevenue += pay;
            DayServed++;
            if (mistake) DayMistakes++;
            FeedSatisfaction(score);
        }

        /// <summary>기다리다 나간 손님 → 0점으로 반영</summary>
        public void RecordLost()
        {
            DayLost++;
            FeedSatisfaction(0f);
        }

        /// <summary>업그레이드 구매. 돈이 모자라면 false</summary>
        public bool TrySpendUpgrade(int cost)
        {
            if (cost < 0 || Coins < cost) return false;
            Coins -= cost;
            DayUpgradeCost += cost;
            Changed?.Invoke();
            return true;
        }

        public void Pay(int amount)
        {
            Coins -= amount;
            Changed?.Invoke();
        }

        void FeedSatisfaction(float score)
        {
            float alpha = GameDatabase.Days != null ? GameDatabase.Days.satisfactionAlpha : 0.2f;
            Satisfaction = Mathf.Clamp(Satisfaction + alpha * (score - Satisfaction), 0f, 100f);
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}