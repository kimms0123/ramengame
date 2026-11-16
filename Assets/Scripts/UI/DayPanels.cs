using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Logic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 영업 준비 / 정산 / 엔딩 패널 (기획서 05, 07).
    /// 이미지: GameArtConfig.panelBackground (9-slice 권장), buttonSprite
    /// </summary>
    public class DayPanels : MonoBehaviour
    {
        RectTransform dim;
        Text title, body;
        Button primary;
        Text primaryLabel;
        DayPhase? shownPhase;
        int shownDay = -1;

        void Awake()
        {
            var art = GameDatabase.Art;
            var layer = UIRoot.Instance.PanelLayer;

            var dimImg = UIFactory.Image(layer, "Dim", null, new Color(0f, 0f, 0f, 0.6f), raycast: true);
            dim = dimImg.rectTransform;
            UIFactory.Stretch(dim);

            var panel = UIFactory.Image(dim, "Panel", art != null ? art.panelBackground : null, UIFactory.Panel);
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 680f));

            Color textColor = art != null && art.panelBackground != null ? UIFactory.Ink : Color.white;

            title = UIFactory.Text(panel.transform, "Title", "", 64, textColor, TextAnchor.UpperCenter);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(840f, 90f));

            body = UIFactory.Text(panel.transform, "Body", "", 32, textColor, TextAnchor.UpperLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 380f));
            body.lineSpacing = 1.15f;

            primary = UIFactory.Button(panel.transform, "Primary", "", OnPrimary, art != null ? art.buttonSprite : null, new Vector2(340f, 90f), 36);
            UIFactory.Place((RectTransform)primary.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(340f, 90f));
            primaryLabel = primary.GetComponentInChildren<Text>();

            dim.gameObject.SetActive(false);
        }

        void Update()
        {
            var dm = DayManager.Instance;
            if (dm == null) return;

            if (shownPhase != dm.Phase || shownDay != dm.Day)
            {
                shownPhase = dm.Phase;
                shownDay = dm.Day;
                Refresh(dm);
            }

            var kb = Keyboard.current;
            if (dim.gameObject.activeSelf && kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                OnPrimary();
        }

        void Refresh(DayManager dm)
        {
            var cfg = GameDatabase.Days;
            var eco = Economy.Instance;
            var db = GameDatabase.Instance;
            var sb = new StringBuilder();

            switch (dm.Phase)
            {
                case DayPhase.Prep:
                {
                    var s = cfg != null ? cfg.Get(dm.Day) : null;
                    title.text = $"DAY {dm.Day}";
                    if (s != null)
                    {
                        sb.AppendLine($"예상 손님      {s.customers}명");
                        sb.AppendLine($"오늘 임대료    {s.rent} C");
                        if (db != null && db.tonkotsu != null) sb.AppendLine($"메뉴            {db.tonkotsu.displayName} 라멘 {db.tonkotsu.menuPrice}C");
                        if (db != null && db.spicy != null && s.spicyChance > 0f) sb.AppendLine($"                  {db.spicy.displayName} 라멘 {db.spicy.menuPrice}C");
                        if (s.rushSeconds > 0f) sb.AppendLine($"<color=#ffb070>마지막 {Mathf.RoundToInt(s.rushSeconds)}초 러시아워!</color>");
                    }
                    sb.AppendLine();
                    if (eco != null) sb.AppendLine($"보유 코인      {eco.Coins} C");
                    if (cfg != null) sb.AppendLine($"갚아야 할 빚  {cfg.debt} C  (DAY {cfg.TotalDays} 종료 후)");
                    body.text = sb.ToString();
                    primaryLabel.text = "영업 시작";
                    dim.gameObject.SetActive(true);
                    break;
                }

                case DayPhase.Settlement:
                {
                    var st = dm.LastSettlement;
                    title.text = $"DAY {st.day} 정산";
                    sb.AppendLine($"매출              +{st.revenue} C");
                    sb.AppendLine($"재료비            -{st.ingredientCost} C");
                    sb.AppendLine($"임대료            -{st.rent} C");
                    sb.AppendLine($"<b>순이익            {(st.net >= 0 ? "+" : "")}{st.net} C</b>");
                    sb.AppendLine();
                    sb.AppendLine($"응대한 손님      {st.served}명  (실수 {st.mistakes})");
                    sb.AppendLine($"놓친 손님         {st.lost}명");
                    sb.AppendLine($"만족도            {Mathf.RoundToInt(st.satisfaction)}");
                    sb.AppendLine($"보유 코인         {st.coinsAfter} C");
                    body.text = sb.ToString();
                    bool last = cfg != null && dm.Day >= cfg.TotalDays;
                    primaryLabel.text = last ? "최종 정산" : "다음 날";
                    dim.gameObject.SetActive(true);
                    break;
                }

                case DayPhase.Ending:
                {
                    title.text = EndingEvaluator.Title(dm.Ending);
                    sb.AppendLine(EndingEvaluator.Description(dm.Ending, dm.EndingByLowSatisfaction));
                    sb.AppendLine();
                    if (eco != null) sb.AppendLine($"최종 잔액   {eco.Coins} C");
                    if (eco != null) sb.AppendLine($"만족도       {Mathf.RoundToInt(eco.Satisfaction)}");
                    body.text = sb.ToString();
                    primaryLabel.text = "처음부터";
                    dim.gameObject.SetActive(true);
                    break;
                }

                default:
                    dim.gameObject.SetActive(false);
                    break;
            }
        }

        int lastPressFrame = -1;

        void OnPrimary()
        {
            // Enter 키와 버튼 Submit이 같은 프레임에 겹치면 두 단계가 넘어가지 않게 한 번만 처리
            if (lastPressFrame == Time.frameCount) return;
            lastPressFrame = Time.frameCount;
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

            var dm = DayManager.Instance;
            if (dm == null) return;

            switch (dm.Phase)
            {
                case DayPhase.Prep: dm.StartBusiness(); break;
                case DayPhase.Settlement: dm.NextDay(); break;
                case DayPhase.Ending: dm.Restart(); break;
            }
        }
    }
}
