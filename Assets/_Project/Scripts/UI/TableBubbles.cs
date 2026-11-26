using System.Collections.Generic;
using System.Text;
using RamenTycoon.Core;
using RamenTycoon.Kitchen;
using TMPro;
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
    [DefaultExecutionOrder(110)]
    public class TableBubbles : MonoBehaviour
    {
        class Entry
        {
            public Table table;
            public TextMeshProUGUI number;
            public RectTransform bubble;
            public RectTransform chips;
            public RectTransform barBack;
            public RectTransform barFill;
            public Image barFillImage;
            public Image status;
            public string signature;
            public Image readyDot;
            public SpriteRenderer tableBody;
        }

        [Tooltip("손님 그림이 없을 때 말풍선 높이 (월드 유닛)")]
        [SerializeField] float bubbleHeight = 3.5f;
        [Tooltip("말풍선 안 라멘 아이콘 크기")]
        [SerializeField] float chipSize = 40f;
        [Tooltip("남은 인내심 게이지 크기 (폭, 높이)")]
        [SerializeField] Vector2 gaugeSize = new(84f, 10f);
        [Tooltip("다 먹은 테이블 위에 띄우는 아이콘 크기")]
        [SerializeField] float statusIconSize = 56f;

        public enum NumberPlacement { BelowTable, AboveTable, Hidden }

        [Header("테이블 번호 (T1, T2 …)")]
        [Tooltip("BelowTable = 테이블 아래 바닥 (손님 얼굴을 안 가림), AboveTable = 테이블 위, Hidden = 안 보이게")]
        [SerializeField] NumberPlacement numberPlacement = NumberPlacement.BelowTable;
        [SerializeField] int numberSize = 22;
        [SerializeField] Color numberColor = new(1f, 0.95f, 0.85f, 0.8f);

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

            e.number = UIFactory.Text(layer, $"T{t.TableNo}_Number", $"T{t.TableNo}", numberSize, numberColor, TextAnchor.MiddleCenter);
            e.number.rectTransform.sizeDelta = new Vector2(80f, 36f);
            UIFactory.SetOutline(e.number, new Color(0f, 0f, 0f, 0.85f));

            // 주문 말풍선
            var bubbleImg = UIFactory.Image(layer, $"T{t.TableNo}_Bubble", art != null ? art.speechBubble : null, new Color(1f, 1f, 1f, 0.94f));
            e.bubble = bubbleImg.rectTransform;
            e.bubble.pivot = new Vector2(0.5f, 0f);
            var v = bubbleImg.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 12, 14);
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

            // 남은 시간은 글자 대신 게이지로 (초록 → 노랑 → 빨강)
            var backImg = UIFactory.Image(e.bubble, "PatienceBack", art != null ? art.patienceBarBack : null, new Color(0.22f, 0.05f, 0.05f, 0.85f));
            e.barBack = backImg.rectTransform;
            var le = backImg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = gaugeSize.y;
            le.preferredWidth = gaugeSize.x;
            e.barFillImage = UIFactory.Image(e.barBack, "Fill", art != null ? art.patienceBarFill : null, new Color(0.4f, 0.85f, 0.35f));
            e.barFill = e.barFillImage.rectTransform;
            e.barFill.anchorMin = Vector2.zero;
            e.barFill.anchorMax = Vector2.one;
            e.barFill.offsetMin = new Vector2(2f, 2f);
            e.barFill.offsetMax = new Vector2(-2f, -2f);
            UIFactory.BubbleTail(e.bubble);

            // 다 먹은 테이블: 글자 대신 더러운 그릇 아이콘
            e.status = UIFactory.Image(layer, $"T{t.TableNo}_DirtyIcon", DirtySprite(art), new Color(0.55f, 0.45f, 0.4f));
            e.status.preserveAspect = true;
            e.status.rectTransform.sizeDelta = new Vector2(statusIconSize, statusIconSize);
            if (art != null && art.dirtyIcon == null && art.bowlDirty == null && art.bowlClean != null)
                e.status.color = new Color(0.55f, 0.48f, 0.42f); // 빈 그릇 그림을 어둡게 = 더러운 그릇
            e.status.gameObject.SetActive(false);

            e.bubble.gameObject.SetActive(false);
            return e;
        }

        void UpdateEntry(Entry e, bool hidden)
        {
            var t = e.table;
            if (t == null) return;
            var layer = UIRoot.Instance.WorldLayer;

            bool showNumber = !hidden && numberPlacement != NumberPlacement.Hidden;
            if (e.number.gameObject.activeSelf != showNumber) e.number.gameObject.SetActive(showNumber);
            Vector3 numWorld = numberPlacement == NumberPlacement.AboveTable ? TableTop(e, 0.35f) : TableBottom(e, 0.3f);
            if (showNumber && UIFactory.WorldToLocal(layer, cam, numWorld, out var numPos))
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

            // 다 먹은 테이블 위에 더러운 그릇 아이콘 (살짝 통통 튀게)
            bool dirty = !hidden && t.State == Table.TableState.Dirty;
            if (e.status.gameObject.activeSelf != dirty) e.status.gameObject.SetActive(dirty);
            if (dirty && UIFactory.WorldToLocal(layer, cam, TableTop(e, 0.5f), out var stPos))
                e.status.rectTransform.anchoredPosition = stPos + Vector2.up * (Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 8f);
        }

        static Sprite DirtySprite(RamenTycoon.Data.GameArtConfig art)
        {
            if (art == null) return null;
            if (art.dirtyIcon != null) return art.dirtyIcon;
            if (art.bowlDirty != null) return art.bowlDirty;
            return art.bowlClean;
        }

        static Vector3 TableBottom(Entry e, float extra)
        {
            float bottom = e.tableBody != null ? e.tableBody.bounds.min.y : e.table.transform.position.y - 0.6f;
            return new Vector3(e.table.transform.position.x, bottom - extra, 0f);
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
            e.readyDot = null;
            if (ticket == null) return;

            // 시안: 구름 말풍선 안에 라멘 그릇 + 남은 시간 (고명 내용은 오른쪽 주문서에)
            var art = GameDatabase.Art;
            var db = GameDatabase.Instance;
            bool spicy = db != null && ticket.Order.Broth == db.spicy;
            Sprite bowl = art == null ? null : (spicy ? art.bowlRamenSpicy : art.bowlRamenTonkotsu);
            if (bowl != null)
            {
                var img = UIFactory.Image(e.chips, "Ramen", bowl, Color.white);
                img.preserveAspect = true;
                var le = img.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = chipSize * 1.4f;
                le.preferredHeight = chipSize * 1.1f;
            }
            else
            {
                UIFactory.IngredientChip(e.chips, ticket.Order.Broth, 1, chipSize);
            }

            // 그릇이 이미 만들어져 배정됐으면 아이콘 오른쪽 위에 초록 점 (글자 없이)
            if (ticket.Made && e.chips.childCount > 0)
            {
                var icon = (RectTransform)e.chips.GetChild(0);
                e.readyDot = UIFactory.Image(icon, "Ready", null, new Color(0.3f, 0.75f, 0.3f));
                e.readyDot.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var rt = e.readyDot.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(14f, 14f);
                rt.anchoredPosition = Vector2.zero;
            }
        }
    }
}