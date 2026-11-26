using RamenTycoon.Audio;
using RamenTycoon.Core;
using RamenTycoon.Data;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 육수 통 (돈코츠 / 매운). 그릇을 들고 와서 상호작용하면 육수를 붓는다.
    /// 부을 때 재료비가 차감된다.
    /// </summary>
    public class BrothUrn : Station
    {
        [SerializeField] IngredientData broth;

        public IngredientData Broth { get { EnsureBroth(); return broth; } }

        [Tooltip("자동 구성 때 매운 육수 통으로 만들지 (오브젝트 이름에 Spicy가 있어도 매운 통)")]
        [SerializeField] bool spicyUrn;

        bool IsSpicyUrn => spicyUrn || name.ToLowerInvariant().Contains("spicy");

        protected override Sprite DefaultSprite(GameArtConfig art) => IsSpicyUrn ? art.brothUrnSpicy : art.brothUrnTonkotsu;
        protected override float DefaultWidth => 1.53f;

#if UNITY_EDITOR
        protected override void OnEditorSetup(GameArtConfig art, Vector2 size)
        {
            var db = GameDatabase.Current;
            if (broth == null && db != null) broth = IsSpicyUrn ? db.spicy : db.tonkotsu;
        }
#endif

        void Start() => EnsureBroth();

        /// <summary>Broth 칸이 비어 있으면 오브젝트 이름(Spicy 포함 여부)으로 데이터베이스의 육수를 연결</summary>
        void EnsureBroth()
        {
            if (broth != null) return;
            var db = GameDatabase.Instance;
            if (db == null) return;
            broth = IsSpicyUrn ? db.spicy : db.tonkotsu;
            if (broth != null) Debug.LogWarning($"[BrothUrn] {name}의 Broth 칸이 비어 있어서 '{broth.displayName}'을(를) 자동으로 연결했어요.");
        }

        public override bool CanInteract(PlayerHand hand)
        {
            EnsureBroth();
            return broth != null && hand.Held is Bowl bowl && bowl.CanTakeBroth;
        }

        public override string GetHint(PlayerHand hand)
        {
            if (hand.IsEmpty) return "싱크대에서 그릇을 먼저 가져와요";
            if (hand.Held is not Bowl bowl) return "그릇을 들고 와야 육수를 담을 수 있어요";
            if (bowl.IsDirty) return "더러운 그릇이에요. 싱크대에 넣어요";
            if (bowl.Broth != null) return "이미 육수가 담겨 있어요";
            if (broth == null) return "육수 데이터가 연결되지 않았어요 (BrothUrn의 Broth 칸 확인)";
            return null;
        }

        public override void Interact(PlayerHand hand)
        {
            if (!CanInteract(hand)) return;
            ((Bowl)hand.Held).AddBroth(broth);
            AudioManager.Play("pour");
            if (Economy.Instance != null) Economy.Instance.SpendIngredient(broth.cost);
        }
    }
}