using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 레벨 선택 창의 레벨 카드 하나입니다. 상점 상품 카드와 같은 구성(정사각 프레임 썸네일 + 이름 + 누르는 박스)을 사용합니다.
    /// 씬이 준비되지 않은 레벨은 자물쇠와 '준비 중'으로 표시하고 누를 수 없게 합니다.
    /// </summary>
    public class UILevelCard : MonoBehaviour
    {
        [SerializeField] private UILevelSelectManager _manager;
        [SerializeField, Range(1, 3)] private int _level = 1;
        [SerializeField, Tooltip("카드에서 실제로 누르는 '출격' 박스 버튼입니다.")]
        private Button _playButton;
        [SerializeField] private TMP_Text _playLabel;
        [SerializeField] private GameObject _lockOverlay;
        [SerializeField, Tooltip("잠겼을 때 흐리게 만들 썸네일입니다.")]
        private Image _thumbnail;
        [SerializeField] private string _playText = "출격";
        [SerializeField] private string _lockedText = "준비 중";

        private void Awake()
        {
            if (_manager == null) _manager = GetComponentInParent<UILevelSelectManager>();
            if (_playButton != null) _playButton.onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            if (_playButton != null) _playButton.onClick.RemoveListener(Play);
        }

        private void OnEnable() => Refresh();

        /// <summary>레벨 씬 준비 여부에 따라 잠김 표시와 버튼 상태를 갱신합니다.</summary>
        public void Refresh()
        {
            bool available = _manager != null && _manager.IsLevelAvailable(_level);
            if (_playButton != null) _playButton.interactable = available;
            if (_playLabel != null) _playLabel.text = available ? _playText : _lockedText;
            if (_lockOverlay != null) _lockOverlay.SetActive(!available);
            if (_thumbnail != null) _thumbnail.color = available ? Color.white : new Color(0.35f, 0.33f, 0.42f, 1f);
        }

        private void Play()
        {
            if (_manager != null) _manager.LevelSelect(_level);
        }
    }
}
