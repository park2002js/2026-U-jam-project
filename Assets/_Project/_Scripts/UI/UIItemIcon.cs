using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 아이템 UI 프리팹의 루트에 부착하여 UI 스크립트가 변경할 내부 아이콘 Image를 지정합니다.
    /// UI 컴포넌트 프리팹에서, 특정 이미지만 변경시켜야 할 때 사용합니다.
    /// </summary>
    public class UIItemIcon : MonoBehaviour
    {
        [SerializeField, Tooltip("프리팹 내부에서 아이템 Sprite를 표시할 자식 Image를 할당합니다. 프레임 Image는 제외합니다.")]
        private Image _icon;

        /// <summary>
        /// UIPlayerItems가 전달한 아이템 Sprite만 적용하고 프레임 등 다른 UI 요소는 유지합니다.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
        }
    }
}
