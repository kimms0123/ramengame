using UnityEngine;

namespace RamenTycoon.Data
{
    public enum UpgradeTarget
    {
        Stove, // 화구
        Sink,  // 설거지 수전
        Plate  // 그릇 수
    }

    [CreateAssetMenu(menuName = "Ramen Tycoon/Upgrade", fileName = "UPG_New")]
    public class UpgradeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public UpgradeTarget target;

        [Tooltip("단계별 비용. costs[0] = 1단계로 올리는 비용")]
        public int[] costs = { 30000, 60000 };

        public int MaxLevel => costs.Length;
    }
}
