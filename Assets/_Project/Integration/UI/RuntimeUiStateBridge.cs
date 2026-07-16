using UnityEngine;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;

namespace UJam.Integration.UI
{
    public sealed class RuntimeUiStateBridge : MonoBehaviour
    {
        // 명시적으로 주입된 PhaseSystem 보관
        private PhaseSystem _phaseSystem;

        // 명시적으로 주입된 Wallet 보관
        private Wallet _wallet;

        // 명시적으로 주입된 PlayerRuntimeBinder 보관
        private PlayerRuntimeBinder _playerBinder;

        // 현재 Runtime 상태 Snapshot 조회
        public RuntimeUiSnapshot State
        {
            get
            {
                // PhaseSystem 연결 여부 확인
                bool hasPhaseSystem = _phaseSystem != null;

                // Wallet 연결 여부 확인
                bool hasWallet = _wallet != null;

                // Player 연결 여부 확인
                bool hasPlayer = _playerBinder != null;

                // 연결된 Phase 값 결정
                PhaseState phase = hasPhaseSystem ? _phaseSystem.CurrentState : default(PhaseState);

                // 연결된 잔액 결정
                long balance = hasWallet ? _wallet.Balance.Value : 0L;

                // 연결된 Player 상태 결정
                PlayerRuntimeState playerState = hasPlayer
                    ? _playerBinder.State
                    : default(PlayerRuntimeState);

                // 현재 값만 담은 Snapshot 반환
                return new RuntimeUiSnapshot(
                    phase,
                    hasPhaseSystem,
                    balance,
                    hasWallet,
                    playerState,
                    hasPlayer);
            }
        }

        // UI 읽기 Provider를 명시적으로 주입
        public void Configure(
            PhaseSystem phaseSystem,
            Wallet wallet,
            PlayerRuntimeBinder playerBinder)
        {
            // PhaseSystem 연결 저장
            _phaseSystem = phaseSystem;

            // Wallet 연결 저장
            _wallet = wallet;

            // PlayerRuntimeBinder 연결 저장
            _playerBinder = playerBinder;
        }
    }
}
