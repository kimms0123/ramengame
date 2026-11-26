using UnityEngine;

namespace RamenTycoon.Data
{
    public enum UpgradeTarget
    {
        Stove, // 화구
        Sink,  // 설거지 수전
        Plate  // 그릇 수
    }

    /// <summary>
    /// 업그레이드 항목 하나 (업그레이드 창의 한 줄).
    /// id가 씬의 UpgradeSlots(Upgrade Id)와 같아야 연결된다. 예: stove → StoveSlots, table → TableSlots
    /// 만들기: Project 창 우클릭 > Create > Ramen Tycoon > Upgrade
    /// </summary>
    [CreateAssetMenu(menuName = "Ramen Tycoon/Upgrade", fileName = "UPG_New")]
    public class UpgradeData : ScriptableObject
    {
        [Tooltip("업그레이드 창 왼쪽 탭 이름. 같은 이름끼리 한 탭에 모임")]
        public string category = "설비";
        [Tooltip("UpgradeSlots의 Upgrade Id와 같게 (stove, table …)")]
        public string id = "stove";
        [Tooltip("창에 보이는 이름")]
        public string displayName = "화구 추가";
        [TextArea(2, 3)]
        [Tooltip("창에 보이는 설명")]
        public string description = "화구를 하나 더 놓아서 면을 동시에 삶을 수 있어요.";
        [Tooltip("창에 보이는 아이콘 (비우면 설비 그림)")]
        public Sprite icon;
        public UpgradeTarget target;

        [Tooltip("단계별 비용 (C). costs[0] = 2번째 칸을 여는 비용, costs[1] = 3번째 칸 …")]
        public int[] costs = { 70, 140 };

        public int MaxLevel => costs.Length;

        /// <summary>현재 단계(1부터)에서 다음 단계로 가는 비용. 더 없으면 -1</summary>
        public int CostFor(int currentLevel)
        {
            int i = currentLevel - 1;
            return costs != null && i >= 0 && i < costs.Length ? costs[i] : -1;
        }
    }
}