using UnityEngine;

namespace RamenTycoon.Core
{
    public enum GameState
    {
        Title,
        RoundStart,
        DayPrep,      // 업그레이드 상점
        Business,     // 영업
        DaySettle,    // 하루 결산 + 자동 저장
        RoundSettle,  // 월세 차감 + 엔딩 판정
        Ending
    }

    /// <summary>
    /// 게임 전체 상태 머신. M1에서는 바로 Business로 시작한다.
    /// M4에서 일차/라운드 진행과 엔딩 판정을 붙인다.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] GameState startState = GameState.Business;

        public GameState State { get; private set; }
        public int Round { get; private set; } = 1;
        public int Day { get; private set; } = 1;

        public const int DaysPerRound = 5;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start() => ChangeState(startState);

        public void ChangeState(GameState next)
        {
            State = next;
            GameEvents.RaiseStateChanged(next);
            Debug.Log($"[GameFlow] {Round}라운드 {Day}일차 → {next}");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}