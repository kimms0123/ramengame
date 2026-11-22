using System.Collections.Generic;
using UnityEngine;

namespace RamenTycoon.Data
{
    /// <summary>
    /// 라운드별 밸런스 값. M0 경제 시트에서 나온 수치를 여기에 넣는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Ramen Tycoon/Round", fileName = "ROUND_01")]
    public class RoundData : ScriptableObject
    {
        [Header("경제")]
        public int rent = 50000;
        public int utility = 10000;
        public int revenueTargetForGourmet = 150000; // 맛집 엔딩 매출 기준

        [Header("영업")]
        public float businessSeconds = 180f;

        [Tooltip("x: 영업 진행도(0~1), y: 분당 손님 도착률")]
        public AnimationCurve arrivalRate = AnimationCurve.Linear(0f, 2f, 1f, 2f);

        public float basePatienceSeconds = 60f;

        [Header("주문")]
        public List<IngredientData> broths = new();
        [Range(0f, 1f)] public float variationChance = 0.1f;
    }
}
