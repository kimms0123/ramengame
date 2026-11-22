using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 플레이어가 손에 들 수 있는 물건의 기반 클래스 (그릇, 재료 등).
    /// </summary>
    public abstract class HoldableItem : MonoBehaviour
    {
        public void AttachTo(Transform anchor)
        {
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;

            // 내려놓은 스테이션/들고 있는 캐릭터보다 앞에 그려지게 한다
            var mine = GetComponent<SpriteRenderer>();
            var owner = anchor.parent != null ? anchor.parent.GetComponentInChildren<SpriteRenderer>() : null;
            if (mine != null && owner != null && owner != mine) mine.sortingOrder = owner.sortingOrder + 3;
        }

        public void Detach()
        {
            transform.SetParent(null, true);
        }

        public virtual string DebugName => name;
    }
}
