using UnityEngine;

namespace RamenTycoon.Core
{
    public enum GameState
    {
        Title,
        RoundStart,
        DayPrep,      // 영업 준비 (시작 패널)
        Business,     // 영업
        DaySettle,    // 하루 정산
        RoundSettle,  // 5일차 종료 후 빚 상환 + 엔딩 판정
        Ending
    }

    /// <summary>
    /// 게임 전체 상태 기록. 실제 진행은 DayManager가 하고, 여기서는 현재 상태/일차를 보관하고 이벤트를 알린다.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Title;
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

        public void SetDay(int day) => Day = Mathf.Max(1, day);

        public void ChangeState(GameState next)
        {
            State = next;
            GameEvents.RaiseStateChanged(next);
            Debug.Log($"[GameFlow] {Day}일차 → {next}");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
