using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 들 수 있는 물건을 생성한다. 이미지는 각 아이템이 GameArtConfig / IngredientData에서 직접 가져온다.
    /// </summary>
    public class ItemFactory : MonoBehaviour
    {
        public static ItemFactory Instance { get; private set; }

        [Tooltip("이미지가 없을 때 쓰는 흰 네모")]
        [SerializeField] Sprite placeholderSprite;
        [SerializeField] int itemSortingOrder = 50;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (placeholderSprite == null)
            {
                var tex = Texture2D.whiteTexture;
                placeholderSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            }
        }

        public IngredientItem CreateIngredient(IngredientData data)
        {
            var go = CreateBase(data.displayName);
            var item = go.AddComponent<IngredientItem>();
            item.Init(data);
            return item;
        }

        public Bowl CreateBowl()
        {
            var go = CreateBase("Bowl");
            return go.AddComponent<Bowl>();
        }

        GameObject CreateBase(string itemName)
        {
            var go = new GameObject(itemName);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = placeholderSprite;
            sr.sortingOrder = itemSortingOrder;
            return go;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
