using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 주방 스테이션 기반 클래스.
    /// 구조: 루트(콜라이더 + 이 컴포넌트) / Body(스프라이트) / ItemAnchor(물건 놓는 위치)
    /// 루트는 스케일을 1로 유지해야 위에 놓인 물건 크기가 찌그러지지 않는다.
    ///
    /// 직접 설치하기: 빈 오브젝트에 Add Component로 이 계열 컴포넌트(TrashCan, Stove …)를 붙이거나
    /// Hierarchy 우클릭 > Ramen Tycoon 메뉴를 쓰면 그림·충돌 영역·물건 위치가 자동으로 구성된다.
    /// 다시 맞추고 싶으면 컴포넌트 ⋮ 메뉴 > "자동 구성".
    /// 그리는 순서(앞/뒤)는 Play할 때 위치(아래쪽 y)로 자동 계산되므로 씬에서 자유롭게 옮겨도 된다.
    /// </summary>
    public abstract class Station : MonoBehaviour, IInteractable
    {
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected Transform itemAnchor;

        static readonly Color LockedTint = new(0.35f, 0.35f, 0.38f, 1f);

        Color idleColor;
        Color highlightColor;
        bool colorsReady;

        public bool IsLocked { get; private set; }

        protected virtual void Awake()
        {
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (itemAnchor == null) itemAnchor = transform;
            ApplySortingByPosition();
            PrepareColors();
        }

        /// <summary>
        /// 위치에 맞춰 앞뒤 순서를 다시 계산 (씬에서 옮겨도 플레이어·손님과 겹치는 순서가 맞게).
        /// 자식 그림(냄비, 진행 바, 의자 등)은 같은 만큼 같이 옮겨서 서로의 순서는 유지한다.
        /// </summary>
        void ApplySortingByPosition()
        {
            if (bodyRenderer == null) return;
            int target = SpriteFit.SortOrderForY(bodyRenderer.bounds.min.y + 0.1f);
            int delta = target - bodyRenderer.sortingOrder;
            if (delta == 0) return;
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += delta;
        }

        void PrepareColors()
        {
            if (colorsReady || bodyRenderer == null) return;
            colorsReady = true;

            Color c = bodyRenderer.color;
            // 스프라이트 원본색(흰색 틴트)이면 밝히는 방식으로는 하이라이트가 안 보이므로,
            // 평소에는 살짝 어둡게 두고 하이라이트일 때 원본색으로 돌린다.
            bool isWhiteTint = c.maxColorComponent > 0.95f;
            idleColor = isWhiteTint ? new Color(0.78f, 0.78f, 0.78f, c.a) : c;
            highlightColor = isWhiteTint ? c : Color.Lerp(c, Color.white, 0.35f);
            bodyRenderer.color = idleColor;
        }

        public abstract bool CanInteract(PlayerHand hand);
        public abstract void Interact(PlayerHand hand);

        /// <summary>상호작용이 안 될 때 머리 위에 띄울 안내 문구 (null이면 표시 안 함)</summary>
        public virtual string GetHint(PlayerHand hand) => null;

        public virtual void SetHighlighted(bool on)
        {
            if (bodyRenderer == null || IsLocked) return;
            bodyRenderer.color = on ? highlightColor : idleColor;
        }

        /// <summary>
        /// 업그레이드 전 잠긴 상태. 보이기는 하지만 어둡고, 상호작용/동작하지 않는다.
        /// </summary>
        public void SetLocked(bool locked)
        {
            PrepareColors();
            IsLocked = locked;
            enabled = !locked;
            if (bodyRenderer != null) bodyRenderer.color = locked ? idleColor * LockedTint : idleColor;
        }

        // ───────── 직접 설치용 자동 구성 ─────────

        /// <summary>자동 구성 때 쓸 기본 그림 (GameArtConfig에서)</summary>
        protected virtual Sprite DefaultSprite(GameArtConfig art) => null;

        /// <summary>자동 구성 때 맞출 그림 폭 (월드 유닛, 시안 기준)</summary>
        protected virtual float DefaultWidth => 1.5f;

#if UNITY_EDITOR
        /// <summary>씬 빌더가 직접 조립하는 동안에는 자동 구성을 끈다 (중복 생성 방지)</summary>
        public static bool SuppressAutoSetup;

        // Add Component로 처음 붙일 때 Unity가 자동으로 부른다
        protected virtual void Reset()
        {
            if (!SuppressAutoSetup) EditorSetup();
        }

        [ContextMenu("자동 구성 (그림 · 충돌 영역 · 물건 위치 다시 맞추기)")]
        public void EditorSetup()
        {
            var art = GameDatabase.Art;

            // Body (그림)
            if (bodyRenderer == null)
            {
                var existing = transform.Find("Body");
                var bodyGo = existing != null ? existing.gameObject : new GameObject("Body");
                bodyGo.transform.SetParent(transform, false);
                bodyRenderer = bodyGo.GetComponent<SpriteRenderer>();
                if (bodyRenderer == null) bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            }
            if (bodyRenderer.sprite == null) bodyRenderer.sprite = art != null ? DefaultSprite(art) : null;
            if (bodyRenderer.sprite == null)
            {
                bodyRenderer.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder/square.png");
                bodyRenderer.color = new Color(0.6f, 0.6f, 0.65f);
            }

            Vector2 size = Vector2.one;
            var sprite = bodyRenderer.sprite;
            if (sprite != null)
            {
                Vector2 b = sprite.bounds.size;
                float s = b.x > 0.001f ? DefaultWidth / b.x : 1f;
                bodyRenderer.transform.localScale = new Vector3(s, s, 1f);
                bodyRenderer.transform.localPosition = -(Vector3)((Vector2)sprite.bounds.center * s); // 그림 가운데 = 루트
                size = b * s;
            }
            bodyRenderer.sortingOrder = SpriteFit.SortOrderForY(transform.position.y - size.y * 0.5f + 0.1f);

            // 충돌 영역 (아래쪽 60%: 위쪽은 플레이어가 뒤로 겹쳐 보이게)
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x * 0.92f, size.y * 0.6f);
            col.offset = new Vector2(0f, -size.y * 0.2f);

            // 물건 놓는 위치
            if (itemAnchor == null || itemAnchor == transform)
            {
                var a = transform.Find("ItemAnchor");
                itemAnchor = a != null ? a : new GameObject("ItemAnchor").transform;
                itemAnchor.SetParent(transform, false);
            }
            itemAnchor.localPosition = new Vector3(0f, size.y * 0.15f, 0f);

            OnEditorSetup(art, size);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>스테이션마다 추가로 필요한 부품 (냄비, 진행 바, 손님 자리 등)</summary>
        protected virtual void OnEditorSetup(GameArtConfig art, Vector2 size) { }

        /// <summary>진행 바 (Background + Fill) 만들기</summary>
        protected UI.ProgressBar EditorCreateProgressBar(Vector2 localPos, float width)
        {
            var old = transform.Find("ProgressBar");
            if (old != null) DestroyImmediate(old.gameObject);

            var square = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Placeholder/square.png");
            int order = bodyRenderer != null ? bodyRenderer.sortingOrder + 20 : 20;

            var bar = new GameObject("ProgressBar");
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = localPos;
            bar.transform.localScale = new Vector3(width, width, 1f);

            var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(bar.transform, false);
            bg.transform.localScale = new Vector3(1f, 0.15f, 1f);
            bg.sprite = square;
            bg.color = new Color(0.1f, 0.1f, 0.1f);
            bg.sortingOrder = order;

            var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(bar.transform, false);
            fill.transform.localScale = new Vector3(1f, 0.15f, 1f);
            fill.sprite = square;
            fill.color = new Color(0.4f, 0.85f, 0.35f);
            fill.sortingOrder = order + 1;

            var pb = bar.AddComponent<UI.ProgressBar>();
            pb.SetFill(fill.transform);
            bar.SetActive(false);
            return pb;
        }
#endif
    }
}