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
        }

        public void Detach()
        {
            transform.SetParent(null, true);
        }

        public virtual string DebugName => name;
    }
}
