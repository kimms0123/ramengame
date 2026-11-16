using UnityEngine;

namespace RamenTycoon.Data
{
    public enum IngredientCategory
    {
        Broth,   // 육수 (돈코츠, 매운)
        Noodle,  // 면
        Topping  // 고명 (차슈, 김, 파, 숙주)
    }

    [CreateAssetMenu(menuName = "Ramen Tycoon/Ingredient", fileName = "ING_New")]
    public class IngredientData : ScriptableObject
    {
        public string id;
        public string displayName;
        public IngredientCategory category;

        [Tooltip("월드에 놓였을 때 스프라이트. 비어 있으면 placeholderColor로 네모를 그린다.")]
        public Sprite icon;

        [Tooltip("고명 데코 화면에서 그릇 위에 올라갈 스프라이트")]
        public Sprite decoSprite;

        public Color placeholderColor = Color.white;
        public int cost;
    }
}
