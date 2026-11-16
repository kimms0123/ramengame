using RamenTycoon.Core;
using RamenTycoon.Player;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 상호작용이 안 될 때 플레이어 머리 위에 잠깐 뜨는 안내 문구.
    /// 예: "먼저 면 상자에서 면을 가져와 화구에 넣어요"
    /// </summary>
    public class FloatingHint : MonoBehaviour
    {
        static FloatingHint instance;

        [SerializeField] float showSeconds = 1.8f;

        Text text;
        float timer;
        PlayerHand hand;
        SpriteRenderer body;
        Camera cam;

        public static void Show(string message)
        {
            if (instance == null) return;
            instance.text.text = message;
            instance.timer = instance.showSeconds;
            instance.text.gameObject.SetActive(true);
        }

        void Awake()
        {
            instance = this;
            text = UIFactory.Text(UIRoot.Instance.WorldLayer, "FloatingHint", "", 30, new Color(1f, 0.92f, 0.6f), TextAnchor.LowerCenter);
            text.rectTransform.sizeDelta = new Vector2(900f, 50f);
            text.rectTransform.pivot = new Vector2(0.5f, 0f);
            text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            text.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        void LateUpdate()
        {
            if (!text.gameObject.activeSelf) return;

            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f || InputGate.GameplayBlocked)
            {
                text.gameObject.SetActive(false);
                return;
            }

            if (hand == null) hand = FindAnyObjectByType<PlayerHand>();
            if (cam == null) cam = Camera.main;
            if (hand == null) return;
            if (body == null)
            {
                var t = hand.transform.Find("Body");
                if (t != null) body = t.GetComponent<SpriteRenderer>();
            }

            // 말풍선(재료 표시)보다 조금 더 위에
            float top = body != null ? body.bounds.max.y : hand.transform.position.y + 2f;
            var world = new Vector3(hand.transform.position.x, top + 0.2f, 0f);
            if (UIFactory.WorldToLocal(UIRoot.Instance.WorldLayer, cam, world, out var local))
            {
                // 재료 말풍선이 떠 있으면 그 위에, 화면 위로 넘치면 HUD 바 아래에서 멈춤
                float bubbleTop = HeldItemBubble.LastBubbleTop;
                if (!float.IsNaN(bubbleTop)) local.y = Mathf.Max(local.y, bubbleTop + 6f);
                var size = new Vector2(Mathf.Min(900f, text.preferredWidth + 20f), 50f);
                Vector2 pos = UIFactory.ClampInside(UIRoot.Instance.WorldLayer, local, size, UIFactory.HudTopMargin);
                // 말풍선과 겹치면 말풍선 아래(머리 옆)로 내려 보인다
                if (!float.IsNaN(bubbleTop) && pos.y < bubbleTop + 6f && pos.y > bubbleTop - 120f) pos.y = bubbleTop - 120f - size.y;
                text.rectTransform.anchoredPosition = pos;
            }

            var c = text.color;
            text.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(timer / 0.4f));
        }
    }
}