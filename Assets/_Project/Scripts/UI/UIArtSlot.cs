using RamenTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// UI 이미지 칸 도우미 (씬에 만들어 둔 UI의 Image마다 붙어 있음).
    /// - Image에 그림이 비어 있으면 GameArtConfig의 같은 칸 그림을 자동으로 넣는다.
    /// - Image에 그림을 직접 끌어다 넣으면 임시 색(네모 색)을 흰색으로 바꿔서 그림이 원래 색으로 보이게 하고,
    ///   그림이 없을 때만 쓰던 장식(Fallback Only)과 테두리(Outline)를 끈다.
    /// 지워도 게임은 동작한다 (편의 기능).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class UIArtSlot : MonoBehaviour
    {
        [Tooltip("GameArtConfig의 칸 이름 (예: hudBar). 이 Image가 비어 있으면 그 칸의 그림을 넣음")]
        public string artField;

        [Tooltip("그림이 없을 때 쓰는 임시 색. 그림이 들어오면 흰색으로 바뀜")]
        public Color fallbackColor = Color.white;

        [Tooltip("그림이 없을 때만 보이는 임시 장식 (그림이 들어오면 자동으로 꺼짐)")]
        public GameObject[] fallbackOnly;

        [Tooltip("판·버튼처럼 늘어나는 그림이면 켬: 그림에 Border가 있으면 자동으로 Sliced. 아이콘은 꺼 두기")]
        public bool autoSlice = true;

        Image image;
        Sprite lastSprite;
        static GameDatabase editDb;

        public static UIArtSlot Attach(Image img, string field, Color fallback, params GameObject[] fallbackOnly)
        {
            var slot = img.GetComponent<UIArtSlot>();
            if (slot == null) slot = img.gameObject.AddComponent<UIArtSlot>();
            slot.artField = field;
            slot.fallbackColor = fallback;
            slot.fallbackOnly = fallbackOnly;
            slot.Sync();
            return slot;
        }

        void OnEnable() => Sync();

        void Update()
        {
            if (!Application.isPlaying) Sync();
        }

        public void Sync()
        {
            if (image == null) image = GetComponent<Image>();
            if (image == null) return;

            if (image.sprite == null && !string.IsNullOrEmpty(artField))
            {
                if (GameDatabase.Instance != null) editDb = GameDatabase.Instance;
                else if (editDb == null) editDb = FindAnyObjectByType<GameDatabase>(); // 에디터: 매번 찾지 않도록 저장
                var art = editDb != null ? editDb.art : null;
                var field = art != null ? art.GetType().GetField(artField) : null;
                if (field != null && field.GetValue(art) is Sprite s && s != null)
                {
                    image.sprite = s;
                    image.type = autoSlice && !image.preserveAspect && s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                }
            }

            // 그림이 바뀌었을 때 한 번만: 9-slice 테두리(Border)가 있는 판·버튼 그림이면 Sliced로.
            // 아이콘(Preserve Aspect 켜짐)은 건드리지 않고, 직접 Simple로 바꾼 것도 다시 바꾸지 않는다.
            if (image.sprite != lastSprite)
            {
                lastSprite = image.sprite;
                if (autoSlice && !image.preserveAspect && image.sprite != null &&
                    image.sprite.border != Vector4.zero && image.type == Image.Type.Simple)
                    image.type = Image.Type.Sliced;
            }

            bool has = image.sprite != null;
            if (has && fallbackColor != Color.white && Same(image.color, fallbackColor)) image.color = Color.white;

            if (fallbackOnly != null)
            {
                foreach (var go in fallbackOnly)
                {
                    if (go != null && go.activeSelf == has) go.SetActive(!has);
                }
            }

            var outline = GetComponent<Outline>();
            if (outline != null && outline.enabled == has) outline.enabled = !has;
        }

        static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f && Mathf.Abs(a.a - b.a) < 0.01f;
    }
}