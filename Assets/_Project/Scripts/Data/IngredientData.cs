using UnityEngine;

namespace RamenTycoon.Data
{
    public enum IngredientCategory
    {
        Broth,   // 육수 (돈코츠, 매운)
        Noodle,  // 면
        Topping  // 고명 (차슈, 김, 파, 계란)
    }

    [CreateAssetMenu(menuName = "Ramen Tycoon/Ingredient", fileName = "ING_New")]
    public class IngredientData : ScriptableObject
    {
        [Tooltip("코드에서 찾을 때 쓰는 고유 id (tonkotsu, spicy, noodle, chashu, nori, scallion, egg)")]
        public string id;
        public string displayName;
        public IngredientCategory category;

        [Header("이미지 (비어 있으면 placeholderColor 네모로 표시)")]
        [Tooltip("말풍선/주문서/월드에 쓰이는 작은 아이콘")]
        public Sprite icon;

        [Tooltip("고명 데코 화면에서 그릇 위에 올라가는 이미지. 비어 있으면 icon을 쓴다")]
        public Sprite decoSprite;

        public Color placeholderColor = Color.white;

        [Header("경제")]
        [Tooltip("재료비 (C). 육수는 그릇에 부을 때 차감된다")]
        public int cost;

        [Tooltip("메뉴 판매가 (C). 육수에만 의미가 있다 (돈코츠 라멘 / 매운 라멘)")]
        public int menuPrice;

        public Sprite IconOrNull => icon;
        public Sprite DecoOrIcon => decoSprite != null ? decoSprite : icon;
    }
}
