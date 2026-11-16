using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using RamenTycoon.UI;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 화구. 육수 1개 + 면 1개를 넣으면 자동으로 조리된다.
    /// 조리가 끝나면 깨끗한 그릇을 들고 와서 상호작용하면 라멘이 담긴다.
    /// 상태: Empty → (재료 투입) → Cooking → Done
    /// </summary>
    public class Stove : Station
    {
        public enum StoveState { Empty, Cooking, Done }

        [SerializeField] float cookSeconds = 5f;
        [SerializeField] SpriteRenderer contentRenderer;
        [SerializeField] ProgressBar progressBar;

        static readonly Color DoneTint = new(1f, 0.92f, 0.6f);

        IngredientData broth;
        bool hasNoodle;
        float timer;

        public StoveState State { get; private set; } = StoveState.Empty;

        protected override void Awake()
        {
            base.Awake();
            RefreshVisual();
        }

        public override bool CanInteract(PlayerHand hand)
        {
            switch (State)
            {
                case StoveState.Empty:
                    if (hand.Held is not IngredientItem item) return false;
                    return item.Data.category switch
                    {
                        IngredientCategory.Broth => broth == null,
                        IngredientCategory.Noodle => !hasNoodle,
                        _ => false
                    };
                case StoveState.Done:
                    return hand.Held is Bowl bowl && bowl.State == BowlState.Clean;
                default:
                    return false;
            }
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;

            if (State == StoveState.Empty && hand.Held is IngredientItem)
            {
                var item = (IngredientItem)hand.Release();
                if (item.Data.category == IngredientCategory.Broth) broth = item.Data;
                else hasNoodle = true;
                Destroy(item.gameObject);

                if (broth != null && hasNoodle) StartCooking();
                RefreshVisual();
            }
            else if (State == StoveState.Done && hand.Held is Bowl bowl)
            {
                bowl.FillRamen(broth);
                ResetStove();
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
                RefreshVisual();
            }
        }

        void StartCooking()
        {
            timer = 0f;
            State = StoveState.Cooking;
            if (progressBar != null)
            {
                progressBar.SetVisible(true);
                progressBar.SetValue(0f);
            }
        }

        void ResetStove()
        {
            broth = null;
            hasNoodle = false;
            timer = 0f;
            State = StoveState.Empty;
            RefreshVisual();
        }

        void RefreshVisual()
        {
            if (progressBar != null && State != StoveState.Cooking) progressBar.SetVisible(false);
            if (contentRenderer == null) return;

            bool hasAnything = broth != null || hasNoodle;
            contentRenderer.enabled = hasAnything;
            if (!hasAnything) return;

            Color c = broth != null ? broth.placeholderColor : new Color(1f, 0.95f, 0.7f);
            contentRenderer.color = State == StoveState.Done ? Color.Lerp(c, DoneTint, 0.5f) : c;
        }
    }
}
