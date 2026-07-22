using UnityEngine;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;

namespace UJam.Integration.UI
{
    public sealed class RuntimeUiStateBridge : MonoBehaviour
    {
        // 현재 Phase 조회 대상
        private PhaseSystem _phaseSystem;

        // 현재 재화 조회 대상
        private Wallet _wallet;

        // 현재 Player 행동 권한 조회 대상
        private PlayerStatus _playerStatus;

        // UI가 현재 런타임 값을 한 번에 조회할 Snapshot
        public RuntimeUiSnapshot State
        {
            get
            {
                // 연결된 Phase 또는 안전한 기본 Phase
                PhaseState phase = _phaseSystem != null
                    ? _phaseSystem.CurrentState
                    : PhaseState.Preparation;
                // 연결된 Wallet 또는 0 재화
                long currency = _wallet != null ? _wallet.Currency : 0L;
                // 연결된 Player의 현재 공격 권한
                bool canAttack = _playerStatus != null && _playerStatus.CanAttack;

                // 현재 값 Snapshot 반환
                return new RuntimeUiSnapshot(phase, currency, canAttack);
            }
        }

        // UI 조회에 필요한 런타임 대상 연결
        public void Configure(PhaseSystem phaseSystem, Wallet wallet, PlayerStatus playerStatus)
        {
            // Phase 조회 대상 저장
            _phaseSystem = phaseSystem;
            // Wallet 조회 대상 저장
            _wallet = wallet;
            // Player 상태 조회 대상 저장
            _playerStatus = playerStatus;
        }
    }
}
