using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RamenTycoon.Audio
{
    /// <summary>버튼을 누르면 딸깍 소리 (AudioManager가 모든 버튼에 자동으로 붙임). 소리 이름을 바꾸면 그 버튼만 다른 소리</summary>
    public class UIClickSound : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Resources/Sounds/SFX 안의 파일 이름")]
        public string sound = "ui_click";

        public void OnPointerClick(PointerEventData eventData)
        {
            var b = GetComponent<Selectable>();
            if (b != null && !b.interactable) return;
            AudioManager.Play(sound);
        }
    }
}
