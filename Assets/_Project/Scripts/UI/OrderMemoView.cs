using System.Collections.Generic;
using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 빌지(주문서). 시안처럼 "N번 테이블 주문서" + 아이콘 줄(육수, 고명, ×2)로 들어온 주문을 쌓아 보여준다.
    /// 고명까지 완성된 그릇이 배정된 주문에는 취소선을 긋는다.
    /// 화면 오른쪽(HUD)과 고명 화면 왼쪽에서 같이 쓴다. 내용에 맞춰 세로 길이가 늘어난다.
    /// 이미지: GameArtConfig.memoPaper (9-slice 권장, 없으면 흰 종이)
    /// </summary>
    public class OrderMemoView : MonoBehaviour
    {
        [SerializeField] int titleSize = 22;
        [SerializeField] float iconSize = 44f;

        RectTransform root;
        string lastSignature;

        /// <summary>
        /// parent 기준 왼쪽 위(0~1) 위치에 width 폭의 주문서를 만든다.
        /// </summary>
        public static OrderMemoView Create(RectTransform parent, Vector2 topLeft, float width, string objName = "OrderMemo")
        {
            var art = GameDatabase.Art;
            var paper = UIFactory.Image(parent, objName, art != null ? art.memoPaper : null, new Color(0.96f, 0.96f, 0.98f));
            var rt = paper.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(topLeft.x, 1f - topLeft.y);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, 120f);

            if (art == null || art.memoPaper == null)
                paper.gameObject.AddComponent<Outline>().effectColor = new Color(0.25f, 0.12f, 0.1f, 1f);

            var layout = paper.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 22);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = paper.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var view = paper.gameObject.AddComponent<OrderMemoView>();
            view.root = rt;
            return view;
        }

        void LateUpdate()
        {
            if (root == null) return;
            string sig = Signature();
            if (sig == lastSignature) return;
            lastSignature = sig;
            Rebuild();
        }

        static string Signature()
        {
            var board = OrderBoard.Instance;
            if (board == null) return "none";
            var sb = new StringBuilder();
            foreach (var t in board.Tickets)
            {
                sb.Append(t.TableNo).Append(t.Made).Append(t.Order.Broth != null ? t.Order.Broth.name : "-");
                foreach (var kv in t.Order.Toppings) sb.Append(kv.Key.name).Append(kv.Value);
                sb.Append('|');
            }
            return sb.ToString();
        }

        void Rebuild()
        {
            UIFactory.ClearChildren(root);

            var board = OrderBoard.Instance;
            var tickets = board != null ? new List<OrderTicket>(board.Tickets) : new List<OrderTicket>();
            tickets.Sort((a, b) => a.TableNo.CompareTo(b.TableNo));

            if (tickets.Count == 0)
            {
                var empty = UIFactory.Text(root, "Empty", "주문 없음", titleSize, new Color(0.2f, 0.12f, 0.1f, 0.45f), TextAnchor.MiddleCenter);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = titleSize + 10f;
                return;
            }

            var db = GameDatabase.Instance;
            foreach (var ticket in tickets) CreateEntry(ticket, db);
        }

        void CreateEntry(OrderTicket ticket, GameDatabase db)
        {
            Color ink = ticket.Made ? new Color(0.15f, 0.1f, 0.08f, 0.45f) : new Color(0.12f, 0.08f, 0.06f);

            var entry = UIFactory.Rect(root, $"T{ticket.TableNo}");
            var v = entry.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var title = UIFactory.Text(entry, "Title", $"<b>{ticket.TableNo}번 테이블 주문서</b>", titleSize, ink, TextAnchor.MiddleLeft);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = titleSize + 6f;

            var row = UIFactory.Rect(entry, "Icons");
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = iconSize;

            UIFactory.IngredientChip(row, ticket.Order.Broth, 1, iconSize);
            if (db != null && db.toppings != null)
            {
                foreach (var t in db.toppings)
                {
                    if (t != null && ticket.Order.Toppings.TryGetValue(t, out int n) && n > 0)
                        UIFactory.IngredientChip(row, t, n, iconSize);
                }
            }

            if (ticket.Made)
            {
                // 완성 → 아이콘 흐리게 + 취소선
                foreach (var img in row.GetComponentsInChildren<Image>()) img.color = new Color(img.color.r, img.color.g, img.color.b, 0.4f);

                var strike = UIFactory.Image(entry, "Strike", null, new Color(0.8f, 0.12f, 0.1f, 0.9f));
                strike.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var srt = strike.rectTransform;
                srt.anchorMin = new Vector2(0f, 0.5f);
                srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(0f, 5f);
                srt.anchoredPosition = Vector2.zero;
            }
        }
    }
}
