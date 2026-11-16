using RamenTycoon.Data;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 들 수 있는 물건을 생성한다. M1에서는 네모 플레이스홀더로 만들고,
    /// 아트가 나오면 프리팹 참조로 바꾸고 M2에서 오브젝트 풀링을 붙인다.
    /// </summary>
    public class ItemFactory : MonoBehaviour
    {
        public static ItemFactory Instance { get; private set; }

        [SerializeField] Sprite placeholderSprite;
        [SerializeField] float ingredientScale = 0.45f;
        [SerializeField] float bowlScale = 0.55f;
        [SerializeField] int itemSortingOrder = 5;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public IngredientItem CreateIngredient(IngredientData data)
        {
            bool hasArt = data.icon != null;
            var go = CreateBase(data.displayName, hasArt ? data.icon : placeholderSprite,
                hasArt ? Color.white : data.placeholderColor, ingredientScale);
            var item = go.AddComponent<IngredientItem>();
            item.Init(data);
            return item;
        }

        public Bowl CreateBowl()
        {
            var go = CreateBase("Bowl", placeholderSprite, Color.white, bowlScale);
            return go.AddComponent<Bowl>();
        }

        GameObject CreateBase(string itemName, Sprite sprite, Color color, float scale)
        {
            var go = new GameObject(itemName);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = itemSortingOrder;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
