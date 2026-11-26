using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RamenTycoon.UI
{
    /// <summary>
    /// 업그레이드 카드 한 장의 부품 연결.
    /// 씬의 CardTemplate(꺼져 있음)을 복사해서 카드를 만들기 때문에, 템플릿의 모양·크기·글자를 고치면 모든 카드에 반영된다.
    /// 부품 이름을 바꾸거나 옮겨도 되지만, 지우면 그 부분은 안 보인다.
    /// </summary>
    public class UpgradeCardView : MonoBehaviour
    {
        public Image picture;
        public TextMeshProUGUI title;
        public TextMeshProUGUI level;
        public TextMeshProUGUI description;
        public Button buyButton;
        public Image buyImage;
        public TextMeshProUGUI buyLabel;
    }
}