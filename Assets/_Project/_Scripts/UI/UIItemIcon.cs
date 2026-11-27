using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 아이템 UI 프리팹의 루트에 부착하여 UI 스크립트가 변경할 내부 아이콘 Image를 지정합니다.
    /// 모든 아이템 아이콘은 정사각형이며, 각자 공용 정사각 프레임(Item_Frame)을 가집니다.
    /// </summary>
    public class UIItemIcon : MonoBehaviour
    {
        [SerializeField, Tooltip("프리팹 내부에서 아이템 Sprite를 표시할 자식 Image를 할당합니다. 프레임 Image는 제외합니다.")]
        private Image _icon;
        [SerializeField, Tooltip("공용 정사각 프레임 Image입니다. 잠긴 칸일 때 어둡게 표시합니다.")]
        private Image _frame;
        [SerializeField, Tooltip("보유 한도를 넘는 칸에 표시할 자물쇠 오브젝트입니다.")]
        private GameObject _lockOverlay;
        [SerializeField] private Color _lockedFrameColor = new Color(0.55f, 0.52f, 0.62f, 0.55f);

        private Color _frameColor = Color.white;
        private bool _frameColorCached;

        /// <summary>
        /// UIPlayerItems가 전달한 아이템 Sprite만 적용하고 프레임 등 다른 UI 요소는 유지합니다. null이면 빈 칸으로 표시합니다.
        /// </summary>
        public void SetIcon(Sprite sprite)
        {
            if (_icon == null) return;
            _icon.sprite = sprite;
            _icon.enabled = sprite != null;
        }

        /// <summary>보유 한도를 넘는 칸이면 프레임을 어둡게 하고 자물쇠를 표시합니다.</summary>
        public void SetLocked(bool locked)
        {
            if (_frame != null)
            {
                if (!_frameColorCached)
                {
                    _frameColor = _frame.color;
                    _frameColorCached = true;
                }
                _frame.color = locked ? _lockedFrameColor : _frameColor;
            }
            if (_lockOverlay != null) _lockOverlay.SetActive(locked);
            if (locked) SetIcon(null);
        }
    }
}
