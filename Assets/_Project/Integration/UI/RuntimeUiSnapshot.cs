using UJam.Runtime.Phase;

namespace UJam.Integration.UI
{
    public readonly struct RuntimeUiSnapshot
    {
        // 현재 Phase 값
        public PhaseState Phase { get; }

        // 현재 Wallet 재화
        public long Currency { get; }

        // 현재 Player 공격 가능 여부
        public bool CanAttack { get; }

        // UI Snapshot 생성
        public RuntimeUiSnapshot(PhaseState phase, long currency, bool canAttack)
        {
            // 현재 Phase 저장
            Phase = phase;
            // 현재 재화 저장
            Currency = currency;
            // 현재 공격 가능 여부 저장
            CanAttack = canAttack;
        }
    }
}
