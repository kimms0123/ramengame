using RamenTycoon.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 상단 HUD (기획서 06): 날짜, 영업 남은 시간, 보유 코인 + 빚 목표, 만족도.
    /// 하단 안내: 조작 키. 오른쪽: 빌지(주문서) 항상 표시.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        Text dayText, timeText, coinText, goalText, statusText, satText;
        RectTransform satFill;
        Image satFillImage;
        OrderMemoView memo;

        void Awake()
        {
            var layer = UIRoot.Instance.HudLayer;
            var art = GameDatabase.Art;

            // 상단 바
            var bar = UIFactory.Image(layer, "TopBar", art != null ? art.hudPanel : null, new Color(0.1f, 0.06f, 0.04f, 0.78f));
            UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(1500f, 84f));

            dayText = UIFactory.Text(bar.transform, "Day", "DAY 1 / 5", 38, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(dayText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(260f, 60f));

            var clock = UIFactory.Image(bar.transform, "ClockIcon", art != null ? art.clockIcon : null, new Color(1f, 1f, 1f, 0f));
            clock.preserveAspect = true;
            UIFactory.Place(clock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(48f, 48f));
            timeText = UIFactory.Text(bar.transform, "Time", "04:30", 42, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(timeText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-140f, 0f), new Vector2(150f, 60f));

            var heart = UIFactory.Image(bar.transform, "HeartIcon", art != null ? art.heartIcon : null, new Color(0.95f, 0.4f, 0.45f));
            heart.preserveAspect = true;
            UIFactory.Place(heart.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(36f, 36f));
            var satBack = UIFactory.Image(bar.transform, "SatBack", art != null ? art.patienceBarBack : null, new Color(0f, 0f, 0f, 0.6f));
            UIFactory.Place(satBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(180f, 22f));
            satFillImage = UIFactory.Image(satBack.transform, "SatFill", art != null ? art.patienceBarFill : null, new Color(0.95f, 0.45f, 0.5f));
            satFill = satFillImage.rectTransform;
            satFill.anchorMin = Vector2.zero;
            satFill.anchorMax = new Vector2(0.7f, 1f);
            satFill.offsetMin = satFill.offsetMax = Vector2.zero;
            satText = UIFactory.Text(bar.transform, "SatText", "70", 26, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Place(satText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(276f, 0f), new Vector2(70f, 40f));

            var coin = UIFactory.Image(bar.transform, "CoinIcon", art != null ? art.coinIcon : null, new Color(1f, 0.8f, 0.25f));
            coin.preserveAspect = true;
            UIFactory.Place(coin.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330f, 0f), new Vector2(42f, 42f));
            coinText = UIFactory.Text(bar.transform, "Coins", "60 C", 40, new Color(1f, 0.87f, 0.4f), TextAnchor.MiddleLeft);
            UIFactory.Place(coinText.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-320f, 0f), new Vector2(170f, 60f));
            goalText = UIFactory.Text(bar.transform, "Goal", "빚 400C", 24, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleRight);
            UIFactory.Place(goalText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(150f, 50f));

            // 영업 종료 등 상태 문구
            statusText = UIFactory.Text(layer, "Status", "", 30, new Color(1f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(900f, 44f));
            statusText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            // 하단 조작 안내
            var hint = UIFactory.Text(layer, "Hint", "WASD 이동    E / Space 상호작용    F1 배속", 26, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1200f, 40f));
            hint.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

            // 주문서 (시안: 화면 오른쪽, 홀 위에 항상 표시)
            memo = OrderMemoView.Create(layer, new Vector2(0.795f, 0.28f), 340f, "HudOrderMemo");
        }

        void Update()
        {
            var dm = DayManager.Instance;
            var eco = Economy.Instance;
            var cfg = GameDatabase.Days;

            if (dm != null)
            {
                int total = cfg != null ? cfg.TotalDays : 5;
                dayText.text = $"DAY {dm.Day} / {total}";
                int sec = Mathf.CeilToInt(dm.TimeLeft);
                timeText.text = $"{sec / 60:00}:{sec % 60:00}";
                timeText.color = dm.Phase == DayPhase.Business && sec <= 30 ? new Color(1f, 0.45f, 0.4f) : Color.white;

                string status = "";
                if (dm.Phase == DayPhase.Closing) status = "영업 종료 — 남은 손님을 마저 응대하세요";
                else if (dm.Phase == DayPhase.Business && dm.WaitingOutside > 0) status = $"밖에서 기다리는 손님 {dm.WaitingOutside}명";
                statusText.text = status;
            }

            if (eco != null)
            {
                coinText.text = $"{eco.Coins} C";
                coinText.color = eco.Coins < 0 ? new Color(1f, 0.45f, 0.4f) : new Color(1f, 0.87f, 0.4f);
                float s = eco.Satisfaction / 100f;
                satFill.anchorMax = new Vector2(Mathf.Clamp01(s), 1f);
                if (GameDatabase.Art == null || GameDatabase.Art.patienceBarFill == null)
                    satFillImage.color = Color.Lerp(new Color(0.85f, 0.3f, 0.3f), new Color(0.95f, 0.5f, 0.6f), s);
                satText.text = Mathf.RoundToInt(eco.Satisfaction).ToString();
            }

            if (cfg != null) goalText.text = $"빚 {cfg.debt}C";

            // 영업 준비/정산 패널, 고명 화면이 떠 있을 때는 숨김 (고명 화면에는 자체 주문서가 있음)
            bool showMemo = !InputGate.GameplayBlocked;
            if (memo.gameObject.activeSelf != showMemo) memo.gameObject.SetActive(showMemo);
        }
    }
}