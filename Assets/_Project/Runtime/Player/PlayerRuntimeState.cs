using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;

namespace UJam.Runtime.Player
{
    public readonly struct PlayerRuntimeState
    {
        // 현재 Player가 읽을 수 있는 Phase 상태
        public PhaseState Phase { get; }

        // PhaseSystem 주입 여부
        public bool HasPhaseSystem { get; }

        // Grid 조회 포트 주입 여부
        public bool HasGridAreaQuery { get; }

        // 공격 대상 Provider 주입 여부
        public bool HasTargetProvider { get; }

        // Player Health 주입 여부
        public bool HasHealth { get; }

        // 현재 상태에서 공격 요청을 처리할 수 있는지 여부
        public bool CanAttack { get; }

        // 주입된 의존성으로 Player 실행 상태를 계산
        public PlayerRuntimeState(
            PhaseSystem phaseSystem,
            IGridAreaQuery gridAreaQuery,
            IPlayerAttackTargetProvider targetProvider,
            Health health)
        {
            // PhaseSystem 존재 여부에 따라 현재 Phase를 결정
            HasPhaseSystem = phaseSystem != null;
            Phase = HasPhaseSystem ? phaseSystem.CurrentState : default(PhaseState);

            // 런타임 의존성 존재 여부를 기록
            HasGridAreaQuery = gridAreaQuery != null;
            HasTargetProvider = targetProvider != null;
            HasHealth = health != null;

            // 모든 공격 조건과 Health 생존 상태를 함께 판정
            CanAttack = HasPhaseSystem
                && Phase == PhaseState.Combat
                && HasGridAreaQuery
                && HasTargetProvider
                && HasHealth
                && !health.IsDead;
        }
    }
}
