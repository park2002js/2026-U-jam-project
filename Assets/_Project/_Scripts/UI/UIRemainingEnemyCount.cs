using TMPro;
using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Wave_RemainingEnemy_UI에 부착하여 PhaseSystem이 전달한 남은 적 수를 표시합니다.
    /// </summary>
    public class UIRemainingEnemyCount : MonoBehaviour
    {
        [SerializeField] private PhaseSystem _phaseSystem;
        [SerializeField] private TMP_Text _countText;

        /// <summary>
        /// PhaseSystem의 적 수 갱신 이벤트를 구독하고, 최초 표시와 재활성화 시 현재 수치를 반영합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_phaseSystem != null) _phaseSystem.OnRemainingEnemyCountChanged += OnSetCount;
            OnSetCount(_phaseSystem != null ? _phaseSystem.RemainingEnemyCount : 0);
        }

        /// <summary>
        /// UI가 닫히거나 제거되면 PhaseSystem의 적 수 갱신 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_phaseSystem != null) _phaseSystem.OnRemainingEnemyCountChanged -= OnSetCount;
        }

        /// <summary>
        /// PhaseSystem이 전달한 수를 표시하며, 기본값과 남은 적이 없는 경우에는 '-'를 표시합니다.
        /// </summary>
        private void OnSetCount(int remainingEnemyCount)
        {
            if (_countText != null) _countText.text = remainingEnemyCount > 0 ? remainingEnemyCount.ToString() : "-";
        }
    }
}
