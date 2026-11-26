using System.Collections.Generic;
using RamenTycoon.Audio;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Kitchen;
using RamenTycoon.Logic;
using RamenTycoon.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 1일차 첫 손님 때 나오는 튜토리얼. 한 행동을 끝내야 다음 안내 창이 뜬다.
    /// 창 흐름: '손님을 기다려 보세요' 창 → 몇 초 뒤 사라짐 → 손님이 주문하면 다음 창 → 해내면 창이 닫히고 다음 창 …
    /// 화면에 건너뛰기 버튼은 없고, Tab(Skip Key)을 1초 꾹 누르면 건너뛴다.
    ///
    ///  손님 기다리기 → 면 꺼내기 → 화구에 넣어 삶기 → 빈 그릇 들기 → 익은 면 건지기 → 맞는 육수 붓기
    ///  → 토핑대로 가져가기 → 고명 얹기 → 종 울리기 → 서빙 → 끝
    ///
    /// 길 안내:
    ///  - 가야 할 설비 위에 통통 튀는 화살표
    ///  - 플레이어 주변에서 그 설비 쪽을 가리키는 작은 화살표 (가까이 가면 숨음)
    ///  - 토핑 화면에서는 집을 토핑 통 → 그릇 → 종 순서로 화살표
    ///  - 지금 단계의 설비가 아닌 곳에서 E를 누르면 막고 "화살표가 가리키는 곳으로 가요!"
    ///
    /// 튜토리얼 중에는 영업 시계·손님 인내심·새 손님이 멈춘다 (면 삶기는 계속).
    /// 새 게임 1일차마다 나온다. Show Only Once를 켜면 한 번 끝내거나 건너뛴 뒤로는 안 나온다 (⋮ > "튜토리얼 기록 지우기"로 초기화).
    /// 안내 문구는 Step Messages에서 고칠 수 있다. {broth} {toppings} {table} 은 주문 내용으로 바뀐다.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class TutorialGuide : BakedView
    {
        const string DoneKey = "lastorder.tutorial.done";

        [Tooltip("켜면 한 번 끝내거나 건너뛴 뒤로는 다시 안 나옴 (PlayerPrefs에 기록).\n끄면 새 게임 1일차마다 항상 나옴")]
        [SerializeField] bool showOnlyOnce = false;

        [Tooltip("켜면 지금 단계의 설비만 E로 쓸 수 있음 (다른 설비는 막고 안내)")]
        [SerializeField] bool lockOtherStations = true;

        [Header("안내 창")]
        [Tooltip("'손님을 기다려 보세요' 창이 떠 있다가 사라지는 시간 (초). 손님이 주문하면 다음 창이 뜸")]
        [SerializeField] float waitMessageSeconds = 4f;
        [Tooltip("마지막 '잘했어요' 창이 떠 있는 시간 (초)")]
        [SerializeField] float finalMessageSeconds = 6f;
        [Tooltip("한 단계를 끝내고 다음 창이 뜰 때까지 비어 있는 시간 (초)")]
        [SerializeField] float gapSeconds = 0.4f;
        [Tooltip("창이 뜰 때 커지며 나타나는 시간 (초)")]
        [SerializeField] float popSeconds = 0.15f;

        [Header("건너뛰기 (키보드)")]
        [Tooltip("이 키를 꾹 누르고 있으면 튜토리얼을 건너뜀. None이면 건너뛸 수 없음")]
        [SerializeField] Key skipKey = Key.Tab;
        [Tooltip("몇 초 동안 누르고 있어야 건너뛰는지")]
        [SerializeField] float skipHoldSeconds = 1f;

        [Header("단계별 안내 문구 (순서 고정)")]
        [TextArea(2, 3)]
        [SerializeField]
        string[] guideMessages =
        {
            "오늘부터 당신의 가게입니다!\n손님이 들어와서 <b>주문할 때까지 기다려 보세요</b>...",
            "주문이 들어왔어요! 먼저 <b>면을 삶아요</b>.\n화살표가 가리키는 <b>면 상자</b>에서 <b>E</b>를 눌러 면을 꺼내요.",
            "<b>화구</b>로 가서 <b>E</b>를 눌러 면을 넣어요.\n면이 삶아지기 시작해요!",
            "면이 삶아지는 동안 <b>싱크대 옆 빈 그릇</b>에서\n<b>E</b>를 눌러 그릇을 들어요.",
            "면이 다 익었어요! 그릇을 든 채로\n<b>화구</b>에서 <b>E</b>를 눌러 면을 건져요.",
            "이 손님은 <b>{broth}</b>를 주문했어요.\n<b>{broth} 육수 통</b>에서 <b>E</b>를 눌러 육수를 부어요.",
            "이제 고명을 얹을 차례예요. <b>토핑대</b>로 가서\n<b>E</b>를 눌러 고명 화면을 열어요.",
            "주문서대로 고명을 얹어요: <b>{toppings}</b>\n화살표가 가리키는 <b>고명 통을 클릭</b>해 집고 <b>그릇을 클릭</b>해 얹어요.",
            "고명을 다 얹었어요!\n<b>종</b>을 눌러(Enter) 라멘을 완성해요.",
            "라멘 완성! <b>{table}번 테이블</b> 손님에게 가서\n<b>E</b>를 눌러 서빙해요.",
            "잘했어요! 손님이 다 먹으면 <b>빈 그릇을 치워 싱크대</b>에 넣어요.\n돈이 모이면 오른쪽 아래 <b>태블릿(U)</b>에서 화구를 업그레이드할 수 있어요.",
        };

        [Header("상황별 추가 문구")]
        [TextArea(2, 3)]
        [Tooltip("면 담기 단계인데 아직 면이 익는 중일 때")]
        [SerializeField] string noodleCookingMessage = "면이 익는 중이에요... 냄비 위 게이지가 다 차면(띵동!)\n그릇을 든 채로 <b>화구</b>에서 <b>E</b>를 눌러 면을 건져요.";
        [TextArea(2, 3)]
        [Tooltip("토핑을 집고 있을 때")]
        [SerializeField] string toppingHoldingMessage = "<b>{holding}</b>을(를) 집었어요! 이제 <b>그릇을 클릭</b>해 얹어요.\n남은 고명: <b>{left}</b>";
        [Tooltip("다른 설비에서 E를 눌렀을 때 뜨는 말")]
        [SerializeField] string blockedHint = "화살표가 가리키는 곳으로 가요!";

        [Header("설비 위 화살표")]
        [SerializeField] float arrowSize = 64f;
        [Tooltip("설비 그림 위로 얼마나 띄울지 (월드 유닛)")]
        [SerializeField] float arrowHeight = 0.35f;

        [Header("플레이어 주변 방향 화살표")]
        [SerializeField] float compassSize = 44f;
        [Tooltip("플레이어 중심에서 얼마나 떨어져서 돌지 (월드 유닛)")]
        [SerializeField] float compassRadius = 1.6f;
        [Tooltip("플레이어 발에서 몸 가운데까지 높이 (월드 유닛)")]
        [SerializeField] float compassHeight = 1.4f;
        [Tooltip("설비와 이 거리보다 가까우면 방향 화살표를 숨김")]
        [SerializeField] float compassHideDistance = 2.6f;

        [Header("토핑 화면")]
        [Tooltip("토핑 화면이 열려 있을 때 안내판 위치 (위쪽 기준 Y)")]
        [SerializeField] float toppingPanelY = -16f;
        [SerializeField] float screenArrowSize = 80f;

        [Header("씬에 만들어 둔 UI (빌더가 연결, 지우지 마세요)")]
        [SerializeField] RectTransform panel;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] TextMeshProUGUI stepText;
        [Tooltip("예전 버전의 건너뛰기 버튼 (지금은 쓰지 않아서 숨김)")]
        [SerializeField] Button skipButton;
        [SerializeField] RectTransform arrow;
        [SerializeField] RectTransform compass;
        [SerializeField] RectTransform screenArrow;

        enum Step
        {
            WaitOrder, TakeNoodle, CookNoodle, TakeBowl, ScoopNoodle, PourBroth,
            GoTopping, PlaceToppings, RingBell, Serve, Done, Off
        }

        Step step = Step.Off;
        float doneTimer;
        float showDelay;   // > 0이면 이 시간이 지난 뒤 창을 띄움 (단계 사이 빈 시간)
        float hideTimer;   // > 0이면 이 시간이 지나면 창을 숨김
        float popTimer = 1f;
        float skipHold;
        bool served;
        bool startedThisSession;
        int tableNo = -1;
        Order order;
        PlayerHand hand;
        Camera cam;
        Vector2 panelHomePos;
        Transform panelHomeParent;

        static readonly Color PanelColor = new(0.99f, 0.94f, 0.84f, 0.97f);
        static readonly Color Brown = new(0.42f, 0.23f, 0.13f);
        static readonly Color Ink = new(0.17f, 0.1f, 0.07f);
        static readonly Color ArrowColor = new(1f, 0.8f, 0.2f);

        protected override bool IsIntact() =>
            panel != null && messageText != null && stepText != null &&
            arrow != null && compass != null && screenArrow != null;

        protected override void Awake()
        {
            base.Awake();
            panelHomeParent = panel.parent;
            panelHomePos = panel.anchoredPosition;
            panel.gameObject.SetActive(false);
            HideArrows();
        }

        protected override void Bind()
        {
            if (skipButton != null) skipButton.gameObject.SetActive(false); // 화면 버튼으로는 건너뛰지 않음
        }

        void OnEnable() => GameEvents.CustomerServed += OnServed;

        void OnDisable()
        {
            GameEvents.CustomerServed -= OnServed;
            if (step != Step.Off) TutorialState.Clear();
        }

        void OnServed(int table, float score, int pay)
        {
            if (step == Step.Serve && (tableNo < 0 || table == tableNo)) served = true;
        }

        // ───────── 만들기 ─────────

        protected override void Build()
        {
            // 위쪽 안내판 (HUD 바 아래)
            var bg = Track(UIFactory.Image(Root.HudLayer, "TutorialPanel (튜토리얼 안내)", null, PanelColor, raycast: true));
            panel = bg.rectTransform;
            UIFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(980f, 150f));
            var outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = Brown;
            outline.effectDistance = new Vector2(3f, -3f);

            stepText = UIFactory.Text(panel, "Step", "튜토리얼 1 / 9", 22, new Color(Brown.r, Brown.g, Brown.b, 0.8f), TextAnchor.UpperLeft);
            stepText.fontStyle = FontStyles.Bold;
            UIFactory.AnchorTopLeft(stepText.rectTransform, new Rect(0.025f, 0.08f, 0.5f, 0.2f));

            messageText = UIFactory.Text(panel, "Message", guideMessages.Length > 1 ? guideMessages[1] : "", 28, Ink, TextAnchor.MiddleLeft);
            messageText.textWrappingMode = TextWrappingModes.Normal;
            UIFactory.AnchorTopLeft(messageText.rectTransform, new Rect(0.025f, 0.28f, 0.95f, 0.66f));


            var art = GameDatabase.Art;
            Sprite arrowSprite = art != null ? art.tutorialArrow : null;

            // 설비 위 화살표 (말풍선 레이어). 그림은 아래를 가리키는 화살표 기준
            arrow = MakeArrow(Root.WorldLayer, "TutorialArrow (설비 위 화살표)", arrowSprite, arrowSize, new Vector2(0.5f, 0f));
            // 플레이어 주변 방향 화살표 (가운데 기준으로 회전)
            compass = MakeArrow(Root.WorldLayer, "TutorialCompass (플레이어 방향 화살표)", arrowSprite, compassSize, new Vector2(0.5f, 0.5f));
            // 토핑 화면 위 화살표
            screenArrow = MakeArrow(Root.ScreenLayer, "TutorialScreenArrow (토핑 화면 화살표)", arrowSprite, screenArrowSize, new Vector2(0.5f, 0f));

            panel.gameObject.SetActive(false);
            HideArrows();
        }

        RectTransform MakeArrow(RectTransform layer, string objName, Sprite sprite, float size, Vector2 pivot)
        {
            var img = Track(UIFactory.Image(layer, objName, sprite, ArrowColor));
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.pivot = pivot;
            UIArtSlot.Attach(img, nameof(GameArtConfig.tutorialArrow), ArrowColor);
            return rt;
        }

        void HideArrows()
        {
            if (arrow != null) arrow.gameObject.SetActive(false);
            if (compass != null) compass.gameObject.SetActive(false);
            if (screenArrow != null) screenArrow.gameObject.SetActive(false);
        }

        // ───────── 시작 / 끝 ─────────

        [ContextMenu("튜토리얼 기록 지우기 (다음 Play 때 다시 나옴)")]
        void ClearRecord()
        {
            PlayerPrefs.DeleteKey(DoneKey);
            PlayerPrefs.Save();
            Debug.Log("[TutorialGuide] 튜토리얼 기록을 지웠어요.");
        }

        [ContextMenu("지금 튜토리얼 시작 (Play 중 테스트)")]
        void StartNow()
        {
            if (!Application.isPlaying) { Debug.Log("[TutorialGuide] Play 중에 눌러 주세요."); return; }
            if (step != Step.Off) Finish("다시 시작");
            startedThisSession = true;
            Begin();
        }

        void Begin()
        {
            Debug.Log("[TutorialGuide] 튜토리얼 시작");
            served = false;
            tableNo = -1;
            order = null;
            hand = FindAnyObjectByType<PlayerHand>();
            TutorialState.BlockedHint = blockedHint;
            GoTo(Step.WaitOrder, false);
        }

        void Finish(string reason)
        {
            if (step == Step.Off) return;
            Debug.Log($"[TutorialGuide] 튜토리얼 끝 (이유: {reason}, 단계: {step})");
            skipHold = 0f;
            showDelay = hideTimer = 0f;
            step = Step.Off;
            TutorialState.Clear();
            MovePanel(false);
            panel.gameObject.SetActive(false);
            HideArrows();
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>다음 단계로. 지금 창을 닫고 잠깐 뒤에 새 창을 띄운다 (gap=false면 바로 바꿈)</summary>
        void GoTo(Step next, bool gap = true)
        {
            step = next;
            if (step == Step.Done) doneTimer = finalMessageSeconds + (gap ? gapSeconds : 0f);
            ApplyRules();
            RefreshText();

            panel.gameObject.SetActive(false);
            showDelay = gap ? Mathf.Max(0.01f, gapSeconds) : 0.01f;
            hideTimer = 0f;
        }

        void UpdatePanelVisibility()
        {
            if (showDelay > 0f)
            {
                showDelay -= Time.unscaledDeltaTime;
                if (showDelay > 0f) return;
                // 새 창 등장
                panel.gameObject.SetActive(true);
                popTimer = 0f;
                AudioManager.Play("ui_open", 0.6f);
                hideTimer = step == Step.WaitOrder ? waitMessageSeconds : 0f; // 행동 안내는 해낼 때까지 떠 있음
            }

            if (hideTimer > 0f)
            {
                hideTimer -= Time.unscaledDeltaTime;
                if (hideTimer <= 0f) panel.gameObject.SetActive(false);
            }

            if (popTimer < 1f && panel.gameObject.activeSelf)
            {
                popTimer = Mathf.Min(1f, popTimer + Time.unscaledDeltaTime / Mathf.Max(0.01f, popSeconds));
                float k = 1f - (1f - popTimer) * (1f - popTimer);
                panel.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
            }
        }

        /// <summary>건너뛰기 키를 꾹 누르면 튜토리얼 종료</summary>
        bool UpdateSkipKey()
        {
            var kb = Keyboard.current;
            if (skipKey == Key.None || kb == null) return false;
            if (kb[skipKey].isPressed)
            {
                skipHold += Time.unscaledDeltaTime;
                if (skipHold >= skipHoldSeconds)
                {
                    Finish("건너뛰기 키");
                    return true;
                }
            }
            else skipHold = 0f;
            return false;
        }

        // ───────── 매 프레임 ─────────

        void Update()
        {
            var dm = DayManager.Instance;
            if (step == Step.Off)
            {
                if (dm == null || dm.Phase != DayPhase.Business || startedThisSession) return;
                startedThisSession = true;
                if (dm.Day != 1)
                {
                    Debug.Log($"[TutorialGuide] 튜토리얼은 1일차에만 나와요. 지금은 DAY {dm.Day} (이어하기로 시작함). " +
                              "새로 보려면 타이틀에서 시작하기를 두 번 누르거나, TutorialGuide ⋮ > 지금 튜토리얼 시작");
                    return;
                }
                if (showOnlyOnce && PlayerPrefs.GetInt(DoneKey, 0) == 1)
                {
                    Debug.Log("[TutorialGuide] 이미 튜토리얼을 본 기록이 있어서 건너뛰어요. (Show Only Once 끄기 또는 ⋮ > 튜토리얼 기록 지우기)");
                    return;
                }
                Begin();
                return;
            }

            // 하루가 끝나거나 엔딩이면 정리
            if (dm == null || dm.Phase == DayPhase.Settlement || dm.Phase == DayPhase.Ending)
            {
                Finish(dm == null ? "DayManager 없음" : $"{dm.Phase} 단계");
                return;
            }

            if (UpdateSkipKey()) return;

            // 토핑 화면을 Esc로 닫았거나 토핑을 뺐으면 한 단계 되돌아간다
            var back = Regress();
            if (back != step) GoTo(back, false);
            // 지금 단계를 해내면 딱 한 단계만 넘어간다
            else if (step < Step.Done && Completed(step)) GoTo(step + 1);

            if (step == Step.Done)
            {
                doneTimer -= Time.unscaledDeltaTime;
                if (doneTimer <= 0f) { Finish("완료"); return; }
            }

            RefreshText(); // 면 익는 중 / 토핑 집는 중처럼 상황에 따라 바뀌는 문구
            UpdatePanelVisibility();
            MovePanel(ToppingOpen);
            UpdateWorldArrows();
            UpdateScreenArrow();
        }

        bool ToppingOpen => ToppingScreen.Instance != null && ToppingScreen.Instance.IsOpen;

        HoldableItem Held
        {
            get
            {
                if (hand == null) hand = FindAnyObjectByType<PlayerHand>();
                return hand != null ? hand.Held : null;
            }
        }

        bool Completed(Step s)
        {
            var held = Held;
            var bowl = held as Bowl;
            switch (s)
            {
                case Step.WaitOrder:
                    var board = OrderBoard.Instance;
                    if (board == null || board.Tickets.Count == 0) return false;
                    tableNo = board.Tickets[0].TableNo;
                    order = board.Tickets[0].Order;
                    return true;
                case Step.TakeNoodle: return held is IngredientItem item && item.Data != null && item.Data.category == IngredientCategory.Noodle;
                case Step.CookNoodle: return held == null && FindStove(st => st.State != Stove.StoveState.Empty) != null;
                case Step.TakeBowl: return bowl != null && bowl.IsEmpty;
                case Step.PourBroth: return bowl != null && bowl.Broth != null;
                case Step.ScoopNoodle: return bowl != null && bowl.HasNoodle;
                case Step.GoTopping: return ToppingOpen;
                case Step.PlaceToppings: return ToppingOpen && TutorialState.ToppingsMatch(ToppingScreen.Instance.Placed);
                case Step.RingBell: return !ToppingOpen && bowl != null && bowl.Decorated;
                case Step.Serve: return served;
                default: return false;
            }
        }

        Step Regress()
        {
            if (step != Step.PlaceToppings && step != Step.RingBell) return step;
            var bowl = Held as Bowl;
            if (!ToppingOpen) return bowl != null && bowl.Decorated ? step : Step.GoTopping; // Esc로 닫음
            if (step == Step.RingBell && !TutorialState.ToppingsMatch(ToppingScreen.Instance.Placed)) return Step.PlaceToppings;
            return step;
        }

        // ───────── 단계별 제한 ─────────

        void ApplyRules()
        {
            // 손님을 받은 뒤부터 끝날 때까지 가게 시간 멈춤
            TutorialState.FreezeShop = step > Step.WaitOrder && step < Step.Done;
            TutorialState.RequiredToppings = step >= Step.GoTopping && step <= Step.RingBell && order != null ? order.Toppings : null;

            if (!lockOtherStations || step == Step.WaitOrder || step >= Step.Done)
            {
                TutorialState.AllowInteract = null;
                return;
            }
            var s = step;
            TutorialState.AllowInteract = target => IsAllowed(s, target);
        }

        bool IsAllowed(Step s, object target)
        {
            switch (s)
            {
                case Step.TakeNoodle: return target is IngredientCrate c && (c.Ingredient == null || c.Ingredient.category == IngredientCategory.Noodle);
                case Step.CookNoodle: return target is Stove;
                case Step.TakeBowl: return target is Sink || target is PlateStack;
                case Step.PourBroth: return target is BrothUrn urn && (order == null || order.Broth == null || urn.Broth == order.Broth);
                case Step.ScoopNoodle: return target is Stove;
                case Step.GoTopping:
                case Step.PlaceToppings:
                case Step.RingBell: return target is ToppingStation;
                case Step.Serve: return target is Table t && (tableNo < 0 || t.TableNo == tableNo);
                default: return true;
            }
        }

        // ───────── 안내 문구 ─────────

        void RefreshText()
        {
            int i = (int)step;
            string msg = i < guideMessages.Length ? guideMessages[i] : "";

            if (step == Step.ScoopNoodle && FindStove(st => st.State == Stove.StoveState.Done) == null)
                msg = noodleCookingMessage;
            if (step == Step.PlaceToppings && ToppingOpen && ToppingScreen.Instance.Holding != null)
                msg = toppingHoldingMessage;

            msg = msg.Replace("{broth}", order != null && order.Broth != null ? order.Broth.displayName : "육수")
                     .Replace("{toppings}", DescribeToppings(false))
                     .Replace("{left}", DescribeToppings(true))
                     .Replace("{table}", tableNo > 0 ? tableNo.ToString() : "주문한")
                     .Replace("{holding}", ToppingOpen && ToppingScreen.Instance.Holding != null ? ToppingScreen.Instance.Holding.displayName : "");
            if (messageText.text != msg) messageText.text = msg;

            int total = (int)Step.Serve;
            string head = step == Step.WaitOrder ? "튜토리얼" : step == Step.Done ? "튜토리얼 완료!" : $"튜토리얼 {i} / {total}";
            if (stepText.text != head) stepText.text = head;
        }

        /// <summary>"차슈×2 · 김". remainingOnly면 아직 안 올린 것만</summary>
        string DescribeToppings(bool remainingOnly)
        {
            if (order == null || order.Toppings.Count == 0) return "없음";
            var placed = remainingOnly && ToppingOpen ? ToppingScreen.Instance.Placed : null;
            var parts = new List<string>();
            foreach (var t in OrderedToppings())
            {
                order.Toppings.TryGetValue(t, out int want);
                int have = 0;
                if (placed != null) placed.TryGetValue(t, out have);
                int n = want - have;
                if (n <= 0) continue;
                parts.Add(n > 1 ? $"{t.displayName}×{n}" : t.displayName);
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : "없음";
        }

        /// <summary>주문 토핑을 토핑 통 순서대로</summary>
        IEnumerable<IngredientData> OrderedToppings()
        {
            var db = GameDatabase.Instance;
            var seen = new HashSet<IngredientData>();
            if (db != null && db.toppings != null)
                foreach (var t in db.toppings)
                    if (t != null && order.Toppings.ContainsKey(t) && seen.Add(t)) yield return t;
            foreach (var kv in order.Toppings)
                if (kv.Key != null && seen.Add(kv.Key)) yield return kv.Key;
        }

        void MovePanel(bool toppingScreen)
        {
            if (panelHomeParent == null) return;
            var parent = toppingScreen ? (Transform)Root.ScreenLayer : panelHomeParent;
            if (panel.parent != parent)
            {
                panel.SetParent(parent, false);
                panel.anchoredPosition = toppingScreen ? new Vector2(panelHomePos.x, toppingPanelY) : panelHomePos;
            }
            if (toppingScreen) panel.SetAsLastSibling();
        }

        // ───────── 화살표: 월드 ─────────

        void UpdateWorldArrows()
        {
            var target = WorldTarget(step);
            bool show = target != null && !InputGate.GameplayBlocked;
            SetActive(arrow, show);
            if (!show)
            {
                SetActive(compass, false);
                return;
            }

            if (cam == null) cam = Camera.main;
            var sr = VisibleRenderer(target);
            Vector3 top = sr != null ? new Vector3(sr.bounds.center.x, sr.bounds.max.y, 0f) : target.position + Vector3.up * 0.8f;
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 0.25f;
            if (UIFactory.WorldToLocal(Root.WorldLayer, cam, top + Vector3.up * (arrowHeight + bob), out var pos))
                arrow.anchoredPosition = pos;

            // 플레이어 주변에서 설비 쪽을 가리키는 화살표
            bool compassOn = false;
            if (hand != null)
            {
                Vector3 center = hand.transform.position + Vector3.up * compassHeight;
                Vector3 goal = sr != null ? sr.bounds.center : target.position;
                Vector2 dir = goal - center;
                float dist = dir.magnitude;
                if (dist > compassHideDistance)
                {
                    dir /= dist;
                    float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.12f;
                    Vector3 at = center + (Vector3)(dir * (compassRadius + pulse));
                    if (UIFactory.WorldToLocal(Root.WorldLayer, cam, at, out var cpos))
                    {
                        compass.anchoredPosition = cpos;
                        // 화살표 그림이 아래(-Y)를 가리키므로 +90도
                        compass.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f);
                        compassOn = true;
                    }
                }
            }
            SetActive(compass, compassOn);
        }

        Transform WorldTarget(Step s)
        {
            switch (s)
            {
                case Step.TakeNoodle: return FindNoodleCrate();
                case Step.CookNoodle:
                    var empty = FindStove(st => st.State == Stove.StoveState.Empty);
                    return empty != null ? empty.transform : null;
                case Step.TakeBowl: return FindBowlSpot();
                case Step.PourBroth: return FindUrnForOrder();
                case Step.ScoopNoodle:
                    var cooked = FindStove(st => st.State == Stove.StoveState.Done) ?? FindStove(st => st.State == Stove.StoveState.Cooking);
                    return cooked != null ? cooked.transform : null;
                case Step.GoTopping:
                case Step.PlaceToppings:
                case Step.RingBell:
                    var ts = FindAnyObjectByType<ToppingStation>();
                    return ts != null ? ts.transform : null;
                case Step.Serve: return FindOrderTable();
                default: return null;
            }
        }

        static SpriteRenderer VisibleRenderer(Transform t)
        {
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>())
                if (sr.enabled && sr.sprite != null) return sr;
            return null;
        }

        static void SetActive(RectTransform rt, bool on)
        {
            if (rt != null && rt.gameObject.activeSelf != on) rt.gameObject.SetActive(on);
        }

        // ───────── 화살표: 토핑 화면 ─────────

        void UpdateScreenArrow()
        {
            RectTransform target = null;
            var screen = ToppingScreen.Instance;
            if (ToppingOpen)
            {
                if (step == Step.PlaceToppings)
                    target = screen.Holding != null ? screen.BowlRect : screen.BinRect(NextNeededTopping());
                else if (step == Step.RingBell)
                    target = screen.BellRect;
            }

            SetActive(screenArrow, target != null);
            if (target == null) return;

            screenArrow.SetAsLastSibling();
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // 0 왼아래, 1 왼위, 2 오른위, 3 오른아래
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            Vector2 local = Root.ScreenLayer.InverseTransformPoint(topCenter);
            float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 18f;
            // 앵커가 가운데이므로 레이어 로컬 좌표 = anchoredPosition
            screenArrow.anchoredPosition = local + Vector2.up * (6f + bob);
        }

        IngredientData NextNeededTopping()
        {
            if (order == null || !ToppingOpen) return null;
            var placed = ToppingScreen.Instance.Placed;
            foreach (var t in OrderedToppings())
            {
                order.Toppings.TryGetValue(t, out int want);
                placed.TryGetValue(t, out int have);
                if (have < want) return t;
            }
            return null;
        }

        // ───────── 설비 찾기 ─────────

        static Stove FindStove(System.Predicate<Stove> match)
        {
            foreach (var st in FindObjectsByType<Stove>(FindObjectsInactive.Exclude))
                if (!st.IsLocked && st.isActiveAndEnabled && match(st)) return st;
            return null;
        }

        static Transform FindNoodleCrate()
        {
            IngredientCrate any = null;
            foreach (var c in FindObjectsByType<IngredientCrate>(FindObjectsInactive.Exclude))
            {
                if (any == null) any = c;
                if (c.Ingredient != null && c.Ingredient.category == IngredientCategory.Noodle) return c.transform;
            }
            return any != null ? any.transform : null;
        }

        static Transform FindBowlSpot()
        {
            var sink = FindAnyObjectByType<Sink>();
            if (sink != null)
            {
                var stack = sink.transform.Find("BowlStack");
                return stack != null ? stack : sink.transform;
            }
            var plates = FindAnyObjectByType<PlateStack>();
            return plates != null ? plates.transform : null;
        }

        Transform FindUrnForOrder()
        {
            IngredientData want = order != null ? order.Broth : null;
            BrothUrn any = null;
            foreach (var urn in FindObjectsByType<BrothUrn>(FindObjectsInactive.Exclude))
            {
                if (any == null) any = urn;
                if (want != null && urn.Broth == want) return urn.transform;
            }
            return any != null ? any.transform : null;
        }

        Transform FindOrderTable()
        {
            foreach (var t in FindObjectsByType<Table>(FindObjectsInactive.Exclude))
                if (t.TableNo == tableNo) return t.transform;
            return null;
        }
    }
}