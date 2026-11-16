using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 움직이는 캐릭터(플레이어, 손님)의 앞뒤 정렬.
    /// 발 위치(이 오브젝트의 y) 기준으로 몸통 Order를 정하고, 자식(들고 있는 그릇 등)은 그 위에 그린다.
    /// </summary>
    public class YSort : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] int childOffset = 3;

        public SpriteRenderer Body
        {
            get => body;
            set => body = value;
        }

        void Awake()
        {
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (body == null) return;
            int order = SpriteFit.SortOrderForY(transform.position.y);
            body.sortingOrder = order;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr != body) sr.sortingOrder = order + childOffset;
            }
        }
    }
}
