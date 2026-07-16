using UnityEngine;
using UJam.Runtime.Phase;

namespace UJam.Integration.UI
{
    public sealed class PhaseUiCommandBridge : MonoBehaviour
    {
        // 마지막 Phase 명령 결과 보관
        public bool LastCommandSucceeded { get; private set; }

        // 명시적으로 주입된 PhaseSystem 보관
        private PhaseSystem _phaseSystem;

        // PhaseSystem을 명시적으로 주입
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            // PhaseSystem 연결 저장
            _phaseSystem = phaseSystem;
        }

        // Combat Phase 시작 명령 전달
        public void StartCombatPhase()
        {
            // 새 UI 호출의 기본 실패 결과 기록
            LastCommandSucceeded = false;

            // PhaseSystem 누락 여부 확인
            if (_phaseSystem == null)
            {
                // 의존성 누락 상태 유지
                return;
            }

            // Runtime 명령을 한 번 생성
            StartCombatCommand command = new StartCombatCommand();

            // Runtime Phase 명령을 한 번 전달
            LastCommandSucceeded = _phaseSystem.TryStartCombat(command);
        }
    }
}
