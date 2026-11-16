using UnityEngine;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 월드 공간 진행 바. 구조: ProgressBar / Background, Fill (둘 다 가로 1 유닛 기준)
    /// Fill은 왼쪽 끝을 기준으로 늘어난다.
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] Transform fill;

        float fillHeight = 0.15f;

        void Awake()
        {
            if (fill != null) fillHeight = fill.localScale.y;
        }

        public void SetValue(float t)
        {
            if (fill == null) return;
            t = Mathf.Clamp01(t);
            fill.localScale = new Vector3(t, fillHeight, 1f);
            fill.localPosition = new Vector3((t - 1f) * 0.5f, 0f, 0f);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
