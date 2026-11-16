using System;
using RamenTycoon.Player;
using UnityEngine;

namespace RamenTycoon.Kitchen
{
    /// <summary>
    /// 깨끗한 그릇 스택. 그릇 총량이 고정되어 있어서 설거지가 밀리면 라멘을 담을 수 없다.
    /// M2에서 개수대가 설거지를 끝내면 AddClean()을 호출한다.
    /// </summary>
    public class PlateStack : Station
    {
        [SerializeField] int count = 6;

        public int Count => count;
        public event Action<int> CountChanged;

        public override bool CanInteract(PlayerHand hand)
        {
            if (hand.IsEmpty) return count > 0;
            return hand.Held is Bowl bowl && bowl.State == BowlState.Clean;
        }

        public override void Interact(PlayerHand hand)
        {
            if (hand.IsEmpty && count > 0)
            {
                hand.Hold(ItemFactory.Instance.CreateBowl());
                SetCount(count - 1);
            }
            else if (hand.Held is Bowl bowl && bowl.State == BowlState.Clean)
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
