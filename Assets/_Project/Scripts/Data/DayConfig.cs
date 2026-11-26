using System;
using UnityEngine;

namespace RamenTycoon.Data
{
    [Serializable]
    public class DaySetting
    {
        [Tooltip("이날 오는 손님 수")]
        public int customers = 6;

        [Tooltip("정산 때 내는 임대료 (C)")]
        public int rent = 25;

        [Tooltip("손님이 음식을 기다려 주는 시간 (초)")]
        public float patience = 60f;

        [Tooltip("매운 라멘 주문 확률 (0이면 돈코츠만)")]
        [Range(0f, 1f)] public float spicyChance = 0.5f;

        [Header("고명 주문 (0 / -1 = 일차에 따라 자동으로 어려워짐)")]
        [Tooltip("주문 하나에 들어가는 고명 종류 수 최소. 0 = 자동 (1일차 1 → 5일차 3)")]
        public int minToppingKinds = 0;

        [Tooltip("주문 하나에 들어가는 고명 종류 수 최대. 0 = 자동 (1일차 2 → 5일차 4)")]
        public int maxToppingKinds = 0;

        [Tooltip("같은 고명을 최대 몇 개까지 시킬지. 0 = 자동 (1일차 1 → 5일차 3)")]
        public int maxToppingCount = 0;

        [Tooltip("고명을 2개 이상 시킬 확률. -1 = 자동 (1일차 0 → 5일차 0.5)")]
        [Range(-1f, 1f)] public float extraCountChance = -1f;

        // 자동 값 (1~5일차, 그 뒤는 5일차와 같음)
        static readonly int[] AutoMinKinds = { 1, 1, 2, 2, 3 };
        static readonly int[] AutoMaxKinds = { 2, 3, 3, 4, 4 };
        static readonly int[] AutoMaxCount = { 1, 2, 2, 3, 3 };
        static readonly float[] AutoExtra = { 0f, 0.2f, 0.3f, 0.4f, 0.5f };

        /// <summary>이날 고명 주문 규칙 (자동 값 반영)</summary>
        public void ToppingRules(int day, out int minKinds, out int maxKinds, out int maxCount, out float extraChance)
        {
            int i = Mathf.Clamp(day - 1, 0, AutoMinKinds.Length - 1);
            minKinds = minToppingKinds > 0 ? minToppingKinds : AutoMinKinds[i];
            maxKinds = maxToppingKinds > 0 ? maxToppingKinds : AutoMaxKinds[i];
            maxCount = maxToppingCount > 0 ? maxToppingCount : AutoMaxCount[i];
            extraChance = extraCountChance >= 0f ? extraCountChance : AutoExtra[i];
            if (maxKinds < minKinds) maxKinds = minKinds;
        }

        [Tooltip("마지막 몇 초를 러시아워로 할지 (그 구간에 손님이 몰림). 0이면 없음")]
        public float rushSeconds = 0f;
    }

    /// <summary>
    /// 하루 진행과 경제 밸런스. 기획서 수치를 기본값으로 넣어 두었다.
    /// </summary>
    [CreateAssetMenu(menuName = "Ramen Tycoon/Day Config", fileName = "DayConfig")]
    public class DayConfig : ScriptableObject
    {
        [Header("하루")]
        [Tooltip("영업 시간 (초). 기획서 4분 30초")]
        public float businessSeconds = 270f;

        [Tooltip("영업 시간이 끝난 뒤 남은 손님을 기다려 주는 최대 시간 (초)")]
        public float closingGraceSeconds = 30f;

        [Tooltip("손님이 식사하는 시간 (초)")]
        public float eatSeconds = 8f;

        [Header("시작 상태")]
        public int startCoins = 60;
        public int activeTables = 4;
        public int activeStoves = 1;

        [Header("엔딩")]
        [Tooltip("5일차 정산 후 갚아야 하는 빚")]
        public int debt = 400;

        [Tooltip("빚을 갚고 이만큼 남으면 인기 맛집 엔딩")]
        public int gourmetRemain = 200;

        [Tooltip("만족도가 이 값보다 낮으면 돈과 관계없이 폐업")]
        public float closeSatisfaction = 30f;

        [Header("만족도")]
        public float startSatisfaction = 70f;

        [Tooltip("손님 1명 점수가 가게 만족도에 반영되는 비율 (지수이동평균)")]
        [Range(0.01f, 1f)] public float satisfactionAlpha = 0.2f;

        [Tooltip("빨리 내줬을 때 최대 팁 비율")]
        [Range(0f, 1f)] public float maxTipRate = 0.2f;

        [Header("가격")]
        [Tooltip("고명 1개당 라멘 값에 더해지는 가격 (C). 재료 에셋의 Menu Price가 0인 고명에 적용")]
        public int toppingPrice = 3;

        [Tooltip("고명 1개당 손님이 더 기다려 주는 시간 (초). 고명이 많은 주문일수록 여유를 줌")]
        public float patiencePerTopping = 2f;

        [Header("일차별 설정 (DAY 1 ~ 5)")]
        public DaySetting[] days =
        {
            new DaySetting { customers = 6,  rent = 25, patience = 60f, spicyChance = 0f },
            new DaySetting { customers = 8,  rent = 30, patience = 57f, spicyChance = 0.35f },
            new DaySetting { customers = 11, rent = 35, patience = 53f, spicyChance = 0.5f },
            new DaySetting { customers = 14, rent = 40, patience = 49f, spicyChance = 0.5f, rushSeconds = 40f },
            new DaySetting { customers = 17, rent = 45, patience = 45f, spicyChance = 0.5f, rushSeconds = 60f },
        };

        public int TotalDays => days.Length;

        public DaySetting Get(int day)
        {
            if (days == null || days.Length == 0) return new DaySetting();
            return days[Mathf.Clamp(day - 1, 0, days.Length - 1)];
        }
    }
}