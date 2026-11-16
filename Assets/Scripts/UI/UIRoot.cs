using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 게임 UI 전체를 코드로 만든다. 씬에는 이 컴포넌트 하나만 두면 된다.
    /// 레이어 순서: World(말풍선) → HUD → Screen(고명 화면) → Panel(시작/정산/엔딩)
    /// 모든 이미지는 GameArtConfig에서 Play 시점에 읽는다.
    /// </summary>
    [DefaultExecutionOrder(100)] // GameDatabase, OrderBoard 등이 준비된 뒤에 UI를 만든다
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        public Canvas Canvas { get; private set; }
        public RectTransform CanvasRect { get; private set; }
        public RectTransform WorldLayer { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform PanelLayer { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCanvas();
            EnsureEventSystem();

            gameObject.AddComponent<HudView>();
            gameObject.AddComponent<HeldItemBubble>();
            gameObject.AddComponent<TableBubbles>();
            gameObject.AddComponent<ToppingScreen>();
            gameObject.AddComponent<DayPanels>();
            gameObject.AddComponent<FloatingHint>();
        }

        void BuildCanvas()
        {
            var go = new GameObject("GameCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            CanvasRect = (RectTransform)go.transform;

            WorldLayer = Layer("WorldLayer");
            HudLayer = Layer("HudLayer");
            ScreenLayer = Layer("ScreenLayer");
            PanelLayer = Layer("PanelLayer");
        }

        RectTransform Layer(string layerName)
        {
            var rt = UIFactory.Rect(CanvasRect, layerName);
            UIFactory.Stretch(rt);
            return rt;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}