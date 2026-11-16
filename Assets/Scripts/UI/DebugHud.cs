using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 그레이박스 테스트용 HUD. 정식 UI가 나오면 제거한다.
    /// F1: 시간 배속 순환(1x → 2x → 0.5x)
    /// </summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] PlayerHand playerHand;
        [SerializeField] PlateStack plateStack;

        int servedCount;
        GUIStyle style;

        void OnEnable() => GameEvents.RamenServed += OnServed;
        void OnDisable() => GameEvents.RamenServed -= OnServed;

        void OnServed(Bowl bowl) => servedCount++;

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f1Key.wasPressedThisFrame && GameClock.Instance != null)
            {
                float s = GameClock.Instance.TimeScale;
                GameClock.Instance.TimeScale = s >= 2f ? 0.5f : s >= 1f ? 2f : 1f;
            }
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 18 };

            string held = playerHand == null || playerHand.IsEmpty ? "없음" : playerHand.Held.DebugName;
            string plates = plateStack != null ? plateStack.Count.ToString() : "-";
            string scale = GameClock.Instance != null ? GameClock.Instance.TimeScale.ToString("0.0") : "1.0";

            GUILayout.BeginArea(new Rect(12, 12, 420, 200), GUI.skin.box);
            GUILayout.Label($"손에 든 것: {held}", style);
            GUILayout.Label($"깨끗한 그릇: {plates}", style);
            GUILayout.Label($"서빙 완료: {servedCount}", style);
            GUILayout.Label($"시간 배속: x{scale}  (F1)", style);
            GUILayout.Label("이동 WASD / 상호작용 Space·E", style);
            GUILayout.EndArea();
        }
    }
}
