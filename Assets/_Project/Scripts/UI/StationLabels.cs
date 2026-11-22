using System.Collections.Generic;
using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 스테이션 이름표. 씬에 따로 배치할 필요 없이 Play하면 자동으로 생긴다.
    /// 기본: 아직 이미지가 없는(네모) 스테이션에만 표시. F2: 전체 표시 / 숨기기 전환.
    /// </summary>
    public class StationLabels : MonoBehaviour
    {
        enum Mode { PlaceholderOnly, All, None }

        [SerializeField] Mode mode = Mode.PlaceholderOnly;

        readonly List<Station> stations = new();
        float refreshTimer;
        Camera cam;
        GUIStyle style, shadow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindAnyObjectByType<StationLabels>() != null) return;
            new GameObject("_StationLabels").AddComponent<StationLabels>();
        }

        void Start() => Refresh();

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame) mode = (Mode)(((int)mode + 1) % 3);

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 1f;
                Refresh();
            }
        }

        void Refresh()
        {
            cam = Camera.main;
            stations.Clear();
            stations.AddRange(FindObjectsByType<Station>());
        }

        void OnGUI()
        {
            if (mode == Mode.None || InputGate.GameplayBlocked || cam == null || Event.current.type != EventType.Repaint) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };
                style.normal.textColor = Color.white;
                shadow = new GUIStyle(style);
                shadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            }

            foreach (var s in stations)
            {
                if (s == null || s is Table) continue; // 테이블은 TableBubbles가 표시
                if (mode == Mode.PlaceholderOnly && HasArt(s)) continue;

                Vector3 sp = cam.WorldToScreenPoint(s.transform.position + Vector3.up * 0.8f);
                if (sp.z < 0f) continue;

                var rect = new Rect(sp.x - 80f, Screen.height - sp.y - 12f, 160f, 24f);
                string text = LabelFor(s);
                GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadow);
                GUI.Label(rect, text, style);
            }
        }

        static bool HasArt(Station s)
        {
            var sr = s.GetComponentInChildren<SpriteRenderer>();
            return sr != null && sr.sprite != null && sr.sprite.name != "square";
        }

        static string LabelFor(Station s)
        {
            switch (s)
            {
                case IngredientCrate crate:
                    return crate.Ingredient != null ? crate.Ingredient.displayName : "재료 통";
                case BrothUrn urn:
                    return urn.Broth != null ? urn.Broth.displayName : "육수 통";
                case Stove stove:
                    return stove.State switch
                    {
                        Stove.StoveState.Cooking => "화구 · 삶는 중",
                        Stove.StoveState.Done => "화구 · 완료!",
                        _ => "화구"
                    };
                case Sink _:
                    return "싱크대 (그릇)";
                case ToppingStation _:
                    return "고명 작업대";
                case PlateStack plates:
                    return $"깨끗한 그릇 ({plates.Count})";
                case Counter _:
                    return "조리대";
                default:
                    return s.name;
            }
        }
    }
}
