using RamenTycoon.Kitchen;
using UnityEngine;

namespace RamenTycoon.Player
{
    /// <summary>
    /// 플레이어의 손. 한 번에 물건 하나만 들 수 있다 (오버쿡드 방식).
    /// </summary>
    public class PlayerHand : MonoBehaviour
    {
        [SerializeField] Transform holdAnchor;

        public HoldableItem Held { get; private set; }
        public bool IsEmpty => Held == null;

        void Awake()
        {
            if (holdAnchor == null) holdAnchor = transform;
        }

        public void Hold(HoldableItem item)
        {
            if (!IsEmpty)
            {
                Debug.LogWarning("[PlayerHand] 이미 물건을 들고 있습니다.");
                return;
            }
            Held = item;
            item.AttachTo(holdAnchor);
        }

        public HoldableItem Release()
        {
            var item = Held;
            Held = null;
            if (item != null) item.Detach();
            return item;
        }
    }
}
