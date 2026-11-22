using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 게임 시간. 손님 인내, 조리 타이머 등 "가게 시간"은 모두 GameClock.DeltaTime을 쓴다.
    /// 고명 데코 중에는 TimeScale만 0.5로 낮추고, UI는 Time.unscaledDeltaTime을 쓴다.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        [SerializeField, Range(0f, 3f)] float timeScale = 1f;
        [SerializeField] bool paused;

        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Mathf.Max(0f, value);
        }

        public bool Paused
        {
            get => paused;
            set => paused = value;
        }

        public static float DeltaTime
        {
            get
            {
                if (Instance == null) return Time.deltaTime;
                return Instance.paused ? 0f : Time.deltaTime * Instance.timeScale;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
