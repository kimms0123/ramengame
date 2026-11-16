using UnityEngine;
using UnityEngine.InputSystem;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 업그레이드로 개수가 늘어나는 스테이션 묶음 (화구, 테이블 등).
    /// slots에 넣은 순서대로 level개까지만 사용할 수 있다.
    /// - Hide: 아직 안 산 칸은 숨김
    /// - ShowLocked: 아직 안 산 칸도 보이지만 어둡게, 사용 불가 (시안처럼 화구 3개가 다 보이게)
    /// 업그레이드 구매 시 TryUpgrade()를 호출하고, 저장/불러오기 때는 SetLevel(n)을 쓴다.
    /// </summary>
    public class UpgradeSlots : MonoBehaviour
    {
        public enum LockedMode { Hide, ShowLocked }

        [Tooltip("켜지는 순서대로 넣는다. 첫 번째는 항상 켜져 있다.")]
        [SerializeField] GameObject[] slots;

        [SerializeField, Min(1)] int startLevel = 1;
        [SerializeField] LockedMode lockedMode = LockedMode.Hide;

        [Tooltip("테스트용 키. Play 중에 누르면 한 단계 올라간다. 쓰지 않으면 None.")]
        [SerializeField] Key debugUpgradeKey = Key.None;

        public int Level { get; private set; }
        public int MaxLevel => slots == null ? 0 : slots.Length;
        public bool IsMax => Level >= MaxLevel;

        // Station.Awake(색 준비)가 끝난 뒤에 적용해야 해서 Start에서 처리한다
        void Start() => SetLevel(startLevel);

        void Update()
        {
            if (debugUpgradeKey == Key.None) return;
            var kb = Keyboard.current;
            if (kb != null && kb[debugUpgradeKey].wasPressedThisFrame) TryUpgrade();
        }

        public bool TryUpgrade()
        {
            if (IsMax) return false;
            SetLevel(Level + 1);
            return true;
        }

        public void SetLevel(int level)
        {
            if (slots == null || slots.Length == 0) return;

            Level = Mathf.Clamp(level, 1, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                bool unlocked = i < Level;

                if (lockedMode == LockedMode.Hide)
                {
                    slots[i].SetActive(unlocked);
                }
                else
                {
                    slots[i].SetActive(true);
                    var station = slots[i].GetComponent<Station>();
                    if (station != null) station.SetLocked(!unlocked);
                }
            }
        }
    }
}