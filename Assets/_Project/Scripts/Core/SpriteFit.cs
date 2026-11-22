using UnityEngine;

namespace RamenTycoon.Core
{
    /// <summary>
    /// 스프라이트의 픽셀 크기/PPU와 상관없이 월드에서 원하는 크기로 맞춘다.
    /// 그래픽 담당이 어떤 해상도로 그려 와도 코드 수정 없이 같은 크기로 보이게 하기 위함.
    /// </summary>
    public static class SpriteFit
    {
        /// <summary>가로 폭을 width 유닛으로 맞춘다 (비율 유지).</summary>
        public static void FitWidth(SpriteRenderer sr, float width)
        {
            if (sr == null || sr.sprite == null) return;
            float w = sr.sprite.bounds.size.x;
            if (w <= 0f) return;
            float s = width / w;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>세로 높이를 height 유닛으로 맞춘다 (비율 유지).</summary>
        public static void FitHeight(SpriteRenderer sr, float height)
        {
            if (sr == null || sr.sprite == null) return;
            float h = sr.sprite.bounds.size.y;
            if (h <= 0f) return;
            float s = height / h;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>스케일이 적용된 월드 크기.</summary>
        public static Vector2 WorldSize(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return Vector2.one;
            Vector3 size = sr.sprite.bounds.size;
            Vector3 scale = sr.transform.lossyScale;
            return new Vector2(size.x * Mathf.Abs(scale.x), size.y * Mathf.Abs(scale.y));
        }

        /// <summary>위아래 정렬용 Order in Layer. 아래(앞)에 있을수록 크다.</summary>
        public static int SortOrderForY(float y) => Mathf.RoundToInt(-y * 10f);
    }
}
