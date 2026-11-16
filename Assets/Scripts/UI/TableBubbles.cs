using System.Collections.Generic;
using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 테이블/손님 머리 위 표시.
    /// - 항상: 테이블 번호 (T1~T4)
    /// - 주문 대기: 주문 말풍선(육수 + 고명) + 인내심 바 (초록 → 노랑 → 빨강)
    /// - 식사 중 / 치우기: 상태 표시
    /// </summary>
    public class TableBubbles : MonoBehaviour
    {
        class Entry
        {
            public Table table;
            public Text number;
            public RectTransform bubble;
            public RectTransform chips;
            public RectTransform barBack;
            public RectTransform barFill;
            public Image barFillImage;
            public Text status;
            public string signature;
            public SpriteRenderer tableBody;
        }

        [SerializeField] float bubbleHeight = 3.5f;
        [SerializeField] float chipSize = 40f;

        readonly List<Entry> entries = new();
        float refreshTimer;
        Camera cam;

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = 0.5f;
                SyncTables();
            }

            bool hidden = InputGate.GameplayBlocked;
            foreach (var e in entries) UpdateEntry(e, hidden);
        }

        void SyncTables()
        {
            var tables = FindObjectsByType<Table>(FindObjectsInactive.Exclude);
            foreach (var t in tables)
            {
                if (entries.Exists(e => e.table == t)) continue;
                entries.Add(CreateEntry(t));
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].table == null || !entries[i].table.isActiveAndEnabled)
                {
                    Destroy(entries[i].number.gameObject);
                    Destroy(entries[i].bubble.gameObject);
                    Destroy(entries[i].status.gameObject);
                    entries.RemoveAt(i);
                }
            }
        }

        Entry CreateEntry(Table t)
        {
            var layer = UIRoot.Instance.WorldLayer;
            var art = GameDatabase.Art;
            var e = new Entry { table = t, tableBody = t.GetComponentInChildren<SpriteRenderer>() };

            e.number = UIFactory.Text(layer, $"T{t.TableNo}_Number", $"T{t.TableNo}", 26, Color.white, TextAnchor.MiddleCenter);
            e.number.rectTransform.sizeDelta = new Vector2(80f, 36f);
            e.number.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            // 주문 말풍선
            var bubbleImg = UIFactory.Image(layer, $"T{t.TableNo}_Bubble", art != null ? art.speechBubble : null, new Color(1f, 1f, 1f, 0.94f));
            e.bubble = bubbleImg.rectTransform;
            e.bubble.pivot = new Vector2(0.5f, 0f);
            var v = bubbleImg.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(12, 12, 8, 10);
            v.spacing = 6f;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            var fit = bubbleImg.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            e.chips = UIFactory.Rect(e.bubble, "Chips");
            var h = e.chips.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var backImg = UIFactory.Image(e.bubble, "PatienceBack", art != null ? art.patienceBarBack : null, new Color(0f, 0f, 0f, 0.35f));
            e.barBack = backImg.rectTransform;
            var le = backImg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 10f;
            le.preferredWidth = 120f;
            e.barFillImage = UIFactory.Image(e.barBack, "Fill", art != null ? art.patienceBarFill : null, new Color(0.4f, 0.85f, 0.35f));
            e.barFill = e.barFillImage.rectTransform;
            e.barFill.anchorMin = Vector2.zero;
            e.barFill.anchorMax = Vector2.one;
            e.barFill.offsetMin = e.barFill.offsetMax = Vector2.zero;

            e.status = UIFactory.Text(layer, $"T{t.TableNo}_Status", "", 28, Color.white, TextAnchor.MiddleCenter);
            e.status.rectTransform.sizeDelta = new Vector2(200f, 40f);
            e.status.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            e.bubble.gameObject.SetActive(false);
            return e;
        }

        void UpdateEntry(Entry e, bool hidden)
        {
            var t = e.table;
            if (t == null) return;
            var layer = UIRoot.Instance.WorldLayer;

            e.number.gameObject.SetActive(!hidden);
            if (!hidden && UIFactory.WorldToLocal(layer, cam, TableTop(e, 0.35f), out var numPos))
                e.number.rectTransform.anchoredPosition = numPos;

            bool waiting = !hidden && t.State == Table.TableState.WaitingFood;
            if (e.bubble.gameObject.activeSelf != waiting) e.bubble.gameObject.SetActive(waiting);

            if (waiting)
            {
                var ticket = OrderBoard.Instance != null ? OrderBoard.Instance.Get(t.TableNo) : null;
                string sig = Signature(ticket);
                if (sig != e.signature)
                {
                    e.signature = sig;
                    RebuildChips(e, ticket);
                }

                float r = t.PatienceRatio;
                e.barFill.anchorMax = new Vector2(r, 1f);
                if (GameDatabase.Art == null || GameDatabase.Art.patienceBarFill == null)
                {
                    e.barFillImage.color = r > 0.5f
                        ? Color.Lerp(new Color(0.95f, 0.8f, 0.25f), new Color(0.4f, 0.85f, 0.35f), (r - 0.5f) * 2f)
                        : Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.95f, 0.8f, 0.25f), r * 2f);
                }

                if (UIFactory.WorldToLocal(layer, cam, CustomerHead(t), out var bubblePos))
                    e.bubble.anchoredPosition = UIFactory.ClampInside(layer, bubblePos, e.bubble.rect.size, UIFactory.HudTopMargin);
            }

            string status = "";
            if (!hidden)
            {
                if (t.State == Table.TableState.Eating) status = "식사 중";
                else if (t.State == Table.TableState.Dirty) status = "치우기!";
            }
            e.status.text = status;
            e.status.color = t.State == Table.TableState.Dirty ? new Color(1f, 0.75f, 0.4f) : Color.white;
            if (status.Length > 0 && UIFactory.WorldToLocal(layer, cam, TableTop(e, 0.9f), out var stPos))
                e.status.rectTransform.anchoredPosition = stPos;
        }

        static Vector3 TableTop(Entry e, float extra)
        {
            float top = e.tableBody != null ? e.tableBody.bounds.max.y : e.table.transform.position.y + 0.6f;
            return new Vector3(e.table.transform.position.x, top + extra, 0f);
        }

        Vector3 CustomerHead(Table t)
        {
            var c = t.Customer;
            if (c != null && c.Body != null && c.Body.sprite != null)
                return new Vector3(c.transform.position.x, c.Body.bounds.max.y + 0.15f, 0f);
            return t.SeatPosition + Vector3.up * bubbleHeight;
        }

        static string Signature(RamenTycoon.Logic.OrderTicket ticket)
        {
            if (ticket == null) return "none";
            var sb = new StringBuilder();
            sb.Append(ticket.Order.Broth != null ? ticket.Order.Broth.name : "-").Append(ticket.Made);
            foreach (var kv in ticket.Order.Toppings) sb.Append(kv.Key.name).Append(kv.Value);
            return sb.ToString();
        }

        void RebuildChips(Entry e, RamenTycoon.Logic.OrderTicket ticket)
        {
            UIFactory.ClearChildren(e.chips);
            if (ticket == null) return;

            var db = GameDatabase.Instance;
            UIFactory.IngredientChip(e.chips, ticket.Order.Broth, 1, chipSize);
            if (db != null && db.toppings != null)
            {
                foreach (var top in db.toppings)
                {
                    if (top != null && ticket.Order.Toppings.TryGetValue(top, out int n) && n > 0)
                        UIFactory.IngredientChip(e.chips, top, n, chipSize);
                }
            }
            if (ticket.Made) UIFactory.TextChip(e.chips, "준비됨", 22, new Color(0.25f, 0.55f, 0.2f));
        }
    }
}