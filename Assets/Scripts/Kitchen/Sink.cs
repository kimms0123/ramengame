using RamenTycoon.Core;
using RamenTycoon.Player;
using RamenTycoon.UI;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 싱크대. (카톡 결정사항) 왼쪽 빈 공간에 쌓인 그릇에서 깨끗한 그릇이 무한으로 나온다.
    /// - 빈손 → 깨끗한 그릇 꺼내기 (무한)
    /// - 더러운 그릇 → 설거지 (washSeconds가 0이면 즉시, 아니면 자동으로 차례대로 씻김)
    /// - 빈 그릇 → 다시 넣기
    /// </summary>
    public class Sink : Station
    {
        [Tooltip("더러운 그릇 1개 씻는 시간. 0이면 넣는 즉시 사라진다")]
        [SerializeField] float washSeconds = 0f;
        [SerializeField] ProgressBar progressBar;

        int dirtyQueue;
        float timer;

        public int DirtyQueue => dirtyQueue;

        protected override void Awake()
        {
            base.Awake();
            if (progressBar != null) progressBar.SetVisible(false);
        }

        public override bool CanInteract(PlayerHand hand)
        {
            if (hand.IsEmpty) return true;
            return hand.Held is Bowl bowl && (bowl.IsDirty || bowl.IsEmpty);
        }

        public override string GetHint(PlayerHand hand)
        {
            if (hand.Held is Bowl) return "음식이 담긴 그릇은 싱크대에 넣을 수 없어요";
            return "빈손으로 오면 그릇을 꺼낼 수 있어요";
        }

        public override void Interact(PlayerHand hand)
        {
            if (hand.IsEmpty)
            {
                hand.Hold(ItemFactory.Instance.CreateBowl());
                return;
            }

            if (hand.Held is Bowl bowl)
            {
                bool dirty = bowl.IsDirty;
                Destroy(hand.Release().gameObject);
                if (dirty && washSeconds > 0f)
                {
                    dirtyQueue++;
                    if (progressBar != null) progressBar.SetVisible(true);
                }
            }
        }

        void Update()
        {
            if (dirtyQueue <= 0) return;

            timer += GameClock.DeltaTime;
            if (progressBar != null) progressBar.SetValue(timer / washSeconds);

            if (timer >= washSeconds)
            {
                timer = 0f;
                dirtyQueue--;
                if (dirtyQueue <= 0 && progressBar != null) progressBar.SetVisible(false);
            }
        }
    }
}