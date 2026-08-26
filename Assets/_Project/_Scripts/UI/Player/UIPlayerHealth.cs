using UnityEngine;
using UnityEngine.UI;
using UJam.Runtime.Player;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Player_HP_UI에 부착하여 PlayerStatus의 체력 변경을 세로 HP바에 표시합니다. 체력 수치는 변경하지 않습니다.
    /// </summary>
    public class UIPlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerStatus _playerStatus;
        [SerializeField] private Image _hpBar;
        [SerializeField, Min(0f), Tooltip("바 전체 길이가 변하는 데 걸리는 시간. 0이면 즉시 반영합니다.")]
        private float _changeDuration = 0.2f;

        private float _targetFill;

        /// <summary>
        /// HP_Bar를 아래에서 위로 채워지는 세로형 Filled 이미지로 설정합니다.
        /// </summary>
        private void Awake()
        {
            if (_hpBar == null) return;
            _hpBar.type = Image.Type.Filled;
            _hpBar.fillMethod = Image.FillMethod.Vertical;
            _hpBar.fillOrigin = (int)Image.OriginVertical.Bottom;
        }
        /// <summary>
        /// 씬의 PlayerStatus.Awake 초기화 이후 최초 체력 표시를 다시 동기화합니다.
        /// </summary>
        private void Start()
        {
            RefreshHealth();
        }

        /// <summary>
        /// PlayerStatus.HealthChanged를 구독하고 다시 열린 UI에 현재 체력을 즉시 표시합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_playerStatus != null) _playerStatus.HealthChanged += OnHealthChanged;
            RefreshHealth();
        }

        /// <summary>
        /// UI가 닫히거나 제거되면 PlayerStatus의 체력 변경 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_playerStatus != null) _playerStatus.HealthChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 실제 체력과 독립적으로 HP바의 표시 비율만 목표 값까지 부드럽게 이동합니다.
        /// </summary>
        private void Update()
        {
            if (_hpBar == null || _hpBar.fillAmount == _targetFill) return;
            _hpBar.fillAmount = _changeDuration > 0f ? Mathf.MoveTowards(_hpBar.fillAmount, _targetFill, Time.unscaledDeltaTime / _changeDuration) : _targetFill;
        }

        /// <summary>
        /// PlayerStatus의 최신 체력을 읽어 숨겨져 있던 동안의 변화도 즉시 반영합니다.
        /// </summary>
        private void RefreshHealth()
        {
            OnHealthChanged(_playerStatus != null ? _playerStatus.CurrentHealth : 0f, _playerStatus != null ? _playerStatus.MaxHealth : 0f);
            if (_hpBar != null) _hpBar.fillAmount = _targetFill;
        }

        /// <summary>
        /// PlayerStatus.HealthChanged가 전달한 현재 체력과 최대 체력으로 HP바의 목표 비율을 갱신합니다.
        /// </summary>
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            _targetFill = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            if (_hpBar != null && _changeDuration <= 0f) _hpBar.fillAmount = _targetFill;
        }
    }
}
