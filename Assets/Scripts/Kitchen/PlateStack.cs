using System;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// (그레이박스용) 개수가 정해진 그릇 스택. LAST ORDER 씬에서는 Sink가 무한 그릇을 대신한다.
    /// </summary>
    public class PlateStack : Station
    {
        [SerializeField] int count = 6;

        public int Count => count;
        public event Action<int> CountChanged;

        public override bool CanInteract(PlayerHand hand)
        {
            if (hand.IsEmpty) return count > 0;
            return hand.Held is Bowl bowl && bowl.IsEmpty;
        }

        public override void Interact(PlayerHand hand)
        {
            if (hand.IsEmpty && count > 0)
            {
                hand.Hold(ItemFactory.Instance.CreateBowl());
                SetCount(count - 1);
            }
            else if (hand.Held is Bowl bowl && bowl.IsEmpty)
            {
                Destroy(hand.Release().gameObject);
                SetCount(count + 1);
            }
        }

        public void AddClean(int amount = 1) => SetCount(count + amount);

        void SetCount(int value)
        {
            count = Mathf.Max(0, value);
            CountChanged?.Invoke(count);
        }
    }
}
