using RamenTycoon.Audio;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using RamenTycoon.UI;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 화구 (1구). 면을 넣으면 자동으로 삶아지고, 다 되면 그릇을 들고 와서 상호작용하면 면을 퍼 담는다.
    /// 그릇에 육수가 먼저 있어도, 나중에 육수를 부어도 상관없다.
    /// 상태: Empty → (면 투입) → Cooking → Done → (그릇에 퍼 담기) → Empty
    /// </summary>
    public class Stove : Station
    {
        public enum StoveState { Empty, Cooking, Done }

        [Tooltip("면 삶는 시간 (초). 화구 3개를 같이 선택하면 한 번에 바꿀 수 있음")]
        [SerializeField] float cookSeconds = 6f;
        [SerializeField] SpriteRenderer contentRenderer;
        [SerializeField] ProgressBar progressBar;

        static readonly Color CookingColor = new(1f, 0.95f, 0.7f);
        static readonly Color DoneColor = new(1f, 0.85f, 0.35f);

        float timer;

        public StoveState State { get; private set; } = StoveState.Empty;
        public float CookSeconds
        {
            get => cookSeconds;
            set => cookSeconds = Mathf.Max(0.5f, value);
        }

        protected override void Awake()
        {
            base.Awake();
            RefreshVisual();
        }

        protected override Sprite DefaultSprite(Data.GameArtConfig art) => art.stove;
        protected override float DefaultWidth => 1.45f;

#if UNITY_EDITOR
        protected override void OnEditorSetup(Data.GameArtConfig art, Vector2 size)
        {
            // 냄비 (삶는 동안만 보임)
            var old = transform.Find("Content");
            if (old != null) DestroyImmediate(old.gameObject);
            var pot = new GameObject("Content").AddComponent<SpriteRenderer>();
            pot.transform.SetParent(transform, false);
            pot.transform.localPosition = new Vector3(0f, size.y * 0.18f, 0f);
            Sprite potSprite = art != null ? art.potCooking : null;
            pot.sprite = potSprite;
            if (potSprite != null)
            {
                float s = size.x * 0.55f / Mathf.Max(0.001f, potSprite.bounds.size.x);
                pot.transform.localScale = new Vector3(s, s, 1f);
            }
            pot.sortingOrder = bodyRenderer.sortingOrder + 1;
            pot.enabled = false;
            contentRenderer = pot;

            progressBar = EditorCreateProgressBar(new Vector2(0f, size.y * 0.5f + 0.3f), size.x * 0.8f);
        }
#endif

        public override bool CanInteract(PlayerHand hand)
        {
            switch (State)
            {
                case StoveState.Empty:
                    return hand.Held is IngredientItem item && item.Data.category == IngredientCategory.Noodle;
                case StoveState.Done:
                    return hand.Held is Bowl bowl && bowl.CanTakeNoodle;
                default:
                    return false;
            }
        }

        public override string GetHint(PlayerHand hand)
        {
            switch (State)
            {
                case StoveState.Empty:
                    if (hand.Held is Bowl) return "먼저 면 상자에서 면을 가져와 화구에 넣어요";
                    if (hand.IsEmpty) return "면 상자에서 면을 가져와요";
                    return "화구에는 면만 넣을 수 있어요";
                case StoveState.Cooking:
                    return "면을 삶는 중이에요";
                default:
                    if (hand.IsEmpty) return "그릇을 들고 와서 면을 담아요";
                    if (hand.Held is Bowl b && !b.CanTakeNoodle) return b.IsDirty ? "더러운 그릇이에요" : "이미 면이 담겨 있어요";
                    return "그릇을 들고 와야 면을 담을 수 있어요";
            }
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (State == StoveState.Empty)
            {
                Destroy(hand.Release().gameObject);
                timer = 0f;
                State = StoveState.Cooking;
                AudioManager.Play("noodle_in");
                if (progressBar != null)
                {
                    progressBar.SetVisible(true);
                    progressBar.SetValue(0f);
                }
                RefreshVisual();
            }
            else if (State == StoveState.Done && hand.Held is Bowl bowl)
            {
                bowl.AddNoodle();
                State = StoveState.Empty;
                AudioManager.Play("scoop");
                RefreshVisual();
            }
        }

        void Update()
        {
            if (State != StoveState.Cooking) return;

            timer += GameClock.DeltaTime;
            if (progressBar != null) progressBar.SetValue(timer / cookSeconds);

            if (timer >= cookSeconds)
            {
                State = StoveState.Done;
                AudioManager.Play("noodle_done");
                RefreshVisual();
            }
        }

        void RefreshVisual()
        {
            if (progressBar != null && State != StoveState.Cooking) progressBar.SetVisible(false);
            if (contentRenderer == null) return;

            contentRenderer.enabled = State != StoveState.Empty;
            if (State == StoveState.Empty) return;

            var art = GameDatabase.Art;
            Sprite pot = art == null ? null : (State == StoveState.Done ? art.potDone : art.potCooking);
            if (pot != null)
            {
                contentRenderer.sprite = pot;
                contentRenderer.color = Color.white;
            }
            else
            {
                contentRenderer.color = State == StoveState.Done ? DoneColor : CookingColor;
            }
        }
    }
}