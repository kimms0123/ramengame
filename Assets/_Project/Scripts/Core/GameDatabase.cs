using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 씬에 하나 두는 데이터 묶음. 모든 시스템은 여기서 재료/설정/이미지를 찾는다.
    /// </summary>
    [DefaultExecutionOrder(-1000)] // UI 등 다른 시스템보다 먼저 준비되어야 한다
    public class GameDatabase : MonoBehaviour
    {
        public static GameDatabase Instance { get; private set; }

        [Header("설정")]
        public GameArtConfig art;
        public DayConfig dayConfig;

        [Header("재료")]
        public IngredientData tonkotsu;
        public IngredientData spicy;
        public IngredientData noodle;

        [Tooltip("고명 순서 = 고명 화면의 통 순서 (차슈, 김, 파, 계란)")]
        public IngredientData[] toppings;

        public static GameArtConfig Art => Instance != null ? Instance.art : null;
        public static DayConfig Days => Instance != null ? Instance.dayConfig : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (art == null) art = ScriptableObject.CreateInstance<GameArtConfig>();
            if (dayConfig == null) dayConfig = ScriptableObject.CreateInstance<DayConfig>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public IngredientData FindTopping(string id)
        {
            if (toppings == null) return null;
            foreach (var t in toppings)
            {
                if (t != null && t.id == id) return t;
            }
            return null;
        }
    }
}
