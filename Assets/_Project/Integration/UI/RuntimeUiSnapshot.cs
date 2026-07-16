using UJam.Runtime.Phase;
using UJam.Runtime.Player;

namespace UJam.Integration.UI
{
    public readonly struct RuntimeUiSnapshot
    {
        // 현재 Phase 값 보관
        public PhaseState Phase { get; }

        // PhaseSystem 연결 여부 보관
        public bool HasPhaseSystem { get; }

        // 현재 잔액 보관
        public long Balance { get; }

        // Wallet 연결 여부 보관
        public bool HasWallet { get; }

        // 현재 Player 상태 보관
        public PlayerRuntimeState PlayerState { get; }

        // Player 연결 여부 보관
        public bool HasPlayer { get; }

        // UI 읽기 전용 Snapshot 구성
        public RuntimeUiSnapshot(
            PhaseState phase,
            bool hasPhaseSystem,
            long balance,
            bool hasWallet,
            PlayerRuntimeState playerState,
            bool hasPlayer)
        {
            // 전달된 Phase 값 저장
            Phase = phase;

            // PhaseSystem 연결 상태 저장
            HasPhaseSystem = hasPhaseSystem;

            // 전달된 잔액 저장
            Balance = balance;

            // Wallet 연결 상태 저장
            HasWallet = hasWallet;

            // 전달된 Player 상태 저장
            PlayerState = playerState;

            // Player 연결 상태 저장
            HasPlayer = hasPlayer;
        }
    }
}
