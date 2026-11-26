using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 게임 UI의 뿌리. 캔버스와 레이어 4개를 갖고 있고, 같은 오브젝트에 화면 컴포넌트들이 붙어 있다.
    /// 레이어 순서: World(말풍선) → HUD → Screen(고명 화면) → Panel(DAY 전환/정산/엔딩/설정)
    ///
    /// 씬 빌더가 에디터에서 UI를 미리 만들어 씬에 저장한다 → Scene 화면에서 직접 옮기고 고칠 수 있다.
    /// 씬에 UI가 없으면 Play 때 코드로 만든다 (예전 방식).
    /// </summary>
    [DefaultExecutionOrder(100)] // GameDatabase, OrderBoard 등이 준비된 뒤
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        [SerializeField] Canvas canvas;
        [SerializeField] RectTransform worldLayer;
        [SerializeField] RectTransform hudLayer;
        [SerializeField] RectTransform screenLayer;
        [SerializeField] RectTransform panelLayer;

        public Canvas Canvas => canvas;
        public RectTransform CanvasRect => canvas != null ? (RectTransform)canvas.transform : null;
        public RectTransform WorldLayer => worldLayer;
        public RectTransform HudLayer => hudLayer;
        public RectTransform ScreenLayer => screenLayer;
        public RectTransform PanelLayer => panelLayer;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureCanvas();
            EnsureEventSystem();
            EnsureViews(); // 씬에 없는 화면만 새로 붙인다 (붙이는 즉시 그 화면의 Awake에서 UI를 만듦)
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void EnsureCanvas()
        {
            if (canvas != null && worldLayer != null && hudLayer != null && screenLayer != null && panelLayer != null) return;

            if (canvas != null) UIFactory.DestroySafe(canvas.gameObject);
            var rt = UIFactory.CreateCanvas(transform, "GameCanvas", 10);
            canvas = rt.GetComponent<Canvas>();
            worldLayer = Layer(rt, "WorldLayer");
            hudLayer = Layer(rt, "HudLayer");
            screenLayer = Layer(rt, "ScreenLayer");
            panelLayer = Layer(rt, "PanelLayer");
        }

        static RectTransform Layer(RectTransform canvasRect, string layerName)
        {
            var rt = UIFactory.Rect(canvasRect, layerName);
            UIFactory.Stretch(rt);
            return rt;
        }

        public void EnsureViews()
        {
            Add<HudView>();
            Add<HeldItemBubble>();
            Add<TableBubbles>();
            Add<ToppingScreen>();
            Add<DayPanels>();
            Add<FloatingHint>();
            Add<EndingScreen>();
            Add<UpgradePanel>();
            Add<TutorialGuide>();
        }

        T Add<T>() where T : Component
        {
            var c = GetComponent<T>();
            if (c == null) c = gameObject.AddComponent<T>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

#if UNITY_EDITOR
        /// <summary>에디터 전용: 캔버스와 모든 화면을 기본 모양으로 새로 만들어 씬에 저장할 수 있게 한다</summary>
        public void EditorBake()
        {
            if (canvas != null) DestroyImmediate(canvas.gameObject);
            canvas = null;
            EnsureCanvas();
            EnsureEventSystem();
            EnsureViews();
            foreach (var view in GetComponents<BakedView>()) view.Rebuild();
            SettingsPanel.Bake(panelLayer);
        }

        /// <summary>에디터 전용: 아직 씬에 없는 화면만 만든다 (이미 만든 UI는 그대로). 새로 만든 개수를 돌려줌</summary>
        public int EditorBakeMissing()
        {
            EnsureCanvas();
            EnsureEventSystem();
            EnsureViews();
            int added = 0;
            foreach (var view in GetComponents<BakedView>())
            {
                if (!view.NeedsBuild) continue; // 이미 있고 멀쩡한 화면은 그대로 (직접 고친 내용 유지)
                view.Rebuild();
                added++;
            }
            if (panelLayer.GetComponentInChildren<SettingsPanel>(true) == null)
            {
                SettingsPanel.Bake(panelLayer);
                added++;
            }
            return added;
        }
#endif
    }
}