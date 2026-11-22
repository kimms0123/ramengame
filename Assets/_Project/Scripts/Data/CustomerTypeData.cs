using UnityEngine;

namespace RamenTycoon.Data
{
    [CreateAssetMenu(menuName = "Ramen Tycoon/Customer Type", fileName = "CUST_New")]
    public class CustomerTypeData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite sprite;

        [Tooltip("1보다 크면 오래 기다려 준다")]
        public float patienceMultiplier = 1f;

        public float tipMultiplier = 1f;

        [Tooltip("RoundData.broths와 같은 순서의 육수 선호 가중치")]
        public float[] brothWeights = { 1f, 1f };
    }
}
