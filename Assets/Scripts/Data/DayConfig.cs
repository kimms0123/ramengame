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

        [Tooltip("변형 주문(차슈 추가, 파 빼기 등) 확률")]
        [Range(0f, 1f)] public float variationChance = 0.1f;

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

        [Header("일차별 설정 (DAY 1 ~ 5)")]
        public DaySetting[] days =
        {
            new DaySetting { customers = 6,  rent = 25, patience = 60f, spicyChance = 0f,   variationChance = 0f },
            new DaySetting { customers = 8,  rent = 30, patience = 57f, spicyChance = 0.35f, variationChance = 0.1f },
            new DaySetting { customers = 10, rent = 35, patience = 53f, spicyChance = 0.5f, variationChance = 0.2f },
            new DaySetting { customers = 12, rent = 40, patience = 49f, spicyChance = 0.5f, variationChance = 0.3f },
            new DaySetting { customers = 14, rent = 45, patience = 45f, spicyChance = 0.5f, variationChance = 0.35f, rushSeconds = 60f },
        };

        public int TotalDays => days.Length;

        public DaySetting Get(int day)
        {
            if (days == null || days.Length == 0) return new DaySetting();
            return days[Mathf.Clamp(day - 1, 0, days.Length - 1)];
        }
    }
}
