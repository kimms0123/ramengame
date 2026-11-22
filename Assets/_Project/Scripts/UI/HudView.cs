using RamenTycoon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 인게임 HUD (그래픽 시안 기준).
    /// - 위쪽 가운데 나무 바: "DAY 1 | 3분 29초" + 코인 "130 / 400 C" + 만족도
    /// - 오른쪽 위: 설정(톱니바퀴) → 설정 창 (열려 있는 동안 게임 멈춤)
    /// - 오른쪽: 주문서(빌지) 항상 표시
    /// - 아래: 조작 안내
    /// 이미지: GameArtConfig.hudBar / coinIcon / heartIcon / settingsIcon
    /// </summary>
    public class HudView : MonoBehaviour
    {
        const float BarWidth = 940f;

        TextMeshProUGUI dayText, coinText, satText, statusText;
        OrderMemoView memo;
        bool pausedBySettings;

        void Awake()
        {
            var layer = UIRoot.Instance.HudLayer;
            var art = GameDatabase.Art;

            // ── 나무 바 ──
            Sprite barSprite = art != null ? art.hudBar : null;
            float barHeight = barSprite != null ? BarWidth * barSprite.rect.height / barSprite.rect.width : 84f;
            var bar = UIFactory.Image(layer, "TopBar", barSprite, new Color(0.38f, 0.23f, 0.13f, 0.92f));
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, barSprite != null ? 14f : -10f), new Vector2(BarWidth, barHeight));

            // 바 안쪽 갈색 영역(이미지 기준 위에서 33%~73%)에 글자를 놓는다
            var inner = UIFactory.Rect(bar.transform, "Inner");
            if (barSprite != null) UIFactory.AnchorTopLeft(inner, new Rect(0.05f, 0.33f, 0.93f, 0.40f));
            else UIFactory.Stretch(inner);

            dayText = UIFactory.Text(inner, "DayTime", "DAY 1  │  4분 30초", 34, Color.white, TextAnchor.MiddleLeft);
            dayText.fontStyle = FontStyles.Bold;
            UIFactory.AnchorTopLeft(dayText.rectTransform, new Rect(0.07f, 0f, 0.45f, 1f));

            var coin = UIFactory.Image(inner, "CoinIcon", art != null ? art.coinIcon : null, new Color(1f, 0.8f, 0.25f));
            coin.preserveAspect = true;
            UIFactory.AnchorTopLeft(coin.rectTransform, new Rect(0.53f, 0.12f, 0.045f, 0.76f));
            coinText = UIFactory.Text(inner, "Coins", "60 / 400 C", 34, Color.white, TextAnchor.MiddleLeft);
            coinText.fontStyle = FontStyles.Bold;
            UIFactory.AnchorTopLeft(coinText.rectTransform, new Rect(0.59f, 0f, 0.25f, 1f));

            var heart = UIFactory.Image(inner, "HeartIcon", art != null ? art.heartIcon : null, new Color(0.95f, 0.45f, 0.5f));
            heart.preserveAspect = true;
            UIFactory.AnchorTopLeft(heart.rectTransform, new Rect(0.855f, 0.22f, 0.03f, 0.56f));
            satText = UIFactory.Text(inner, "Satisfaction", "70", 28, Color.white, TextAnchor.MiddleLeft);
            UIFactory.AnchorTopLeft(satText.rectTransform, new Rect(0.895f, 0f, 0.1f, 1f));

            // ── 설정 버튼 ──
            Sprite gearSprite = art != null ? art.settingsIcon : null;
            var gear = UIFactory.Button(layer, "SettingsButton", gearSprite != null ? "" : "설정", OpenSettings, gearSprite, new Vector2(80f, 80f), 26);
            UIFactory.Place((RectTransform)gear.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(80f, 80f));
            if (gearSprite != null) ((Image)gear.targetGraphic).preserveAspect = true;

            // ── 상태 문구 / 조작 안내 ──
            statusText = UIFactory.Text(layer, "Status", "", 30, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(barHeight + 4f)), new Vector2(900f, 44f));
            UIFactory.SetOutline(statusText, new Color(0f, 0f, 0f, 0.8f));

            var hint = UIFactory.Text(layer, "Hint", "WASD 이동    E / Space 상호작용    Esc 설정    F1 배속", 26, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1200f, 40f));
            UIFactory.SetOutline(hint, new Color(0f, 0f, 0f, 0.8f));

            // ── 주문서 (시안: 화면 오른쪽, 홀 위에 항상 표시) ──
            memo = OrderMemoView.Create(layer, new Vector2(0.795f, 0.28f), 340f, "HudOrderMemo");
        }

        void Update()
        {
            var dm = DayManager.Instance;
            var eco = Economy.Instance;
            var cfg = GameDatabase.Days;

            if (dm != null)
            {
                int sec = Mathf.CeilToInt(dm.TimeLeft);
                string time = sec >= 60 ? $"{sec / 60}분 {sec % 60:00}초" : $"{sec}초";
                dayText.text = $"DAY {dm.Day}  │  {time}";
                dayText.color = dm.Phase == DayPhase.Business && sec <= 30 ? new Color(1f, 0.55f, 0.45f) : Color.white;

                string status = "";
                if (dm.Phase == DayPhase.Closing) status = "영업 종료 — 남은 손님을 마저 응대하세요";
                else if (dm.Phase == DayPhase.Business && dm.WaitingOutside > 0) status = $"밖에서 기다리는 손님 {dm.WaitingOutside}명";
                statusText.text = status;
            }

            if (eco != null)
            {
                int debt = cfg != null ? cfg.debt : 400;
                coinText.text = $"{eco.Coins} / {debt} C";
                coinText.color = eco.Coins < 0 ? new Color(1f, 0.55f, 0.45f) : Color.white;
                satText.text = Mathf.RoundToInt(eco.Satisfaction).ToString();
            }

            // 영업 준비/정산 패널, 고명 화면, 설정 창이 떠 있을 때는 주문서 숨김
            bool showMemo = !InputGate.GameplayBlocked;
            if (memo.gameObject.activeSelf != showMemo) memo.gameObject.SetActive(showMemo);

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && UIFactory.EscConsumedFrame != Time.frameCount
                && !SettingsPanel.IsOpen && CanOpenSettings())
            {
                UIFactory.EscConsumedFrame = Time.frameCount;
                OpenSettings();
            }
        }

        bool CanOpenSettings()
        {
            var dm = DayManager.Instance;
            bool screenOpen = ToppingScreen.Instance != null && ToppingScreen.Instance.IsOpen;
            return !screenOpen && (dm == null || dm.Phase == DayPhase.Business || dm.Phase == DayPhase.Closing);
        }

        void OpenSettings()
        {
            if (SettingsPanel.IsOpen || !CanOpenSettings()) return;

            // 설정 창이 떠 있는 동안 게임을 멈춘다
            pausedBySettings = !InputGate.GameplayBlocked;
            if (pausedBySettings)
            {
                InputGate.GameplayBlocked = true;
                if (GameClock.Instance != null) GameClock.Instance.Paused = true;
            }
            SettingsPanel.Open(UIRoot.Instance.PanelLayer, OnSettingsClosed);
        }

        void OnSettingsClosed()
        {
            if (!pausedBySettings) return;
            pausedBySettings = false;
            InputGate.GameplayBlocked = false;
            if (GameClock.Instance != null) GameClock.Instance.Paused = false;
        }
    }
}
