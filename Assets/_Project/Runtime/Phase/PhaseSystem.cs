using System;

namespace UJam.Runtime.Phase
{
    public sealed class PhaseSystem : IStageEnemyReporter
    {
        // 현재 Phase 상태를 보관
        private PhaseState currentState;

        // 스테이지 번호를 보관
        private readonly int currentStageId;

        // 현재 웨이브 컨트롤러를 보관
        private readonly WaveController waveController;

        // 스테이지 완료 이벤트가 발행되었는지 보관
        private bool stageCompletedPublished;

        // 현재 Phase 상태를 반환
        public PhaseState CurrentState
        {
            get
            {
                // 저장된 Phase 상태를 반환
                return currentState;
            }
        }

        // 현재 스테이지 번호를 반환
        public int CurrentStageId
        {
            get
            {
                // 저장된 스테이지 번호를 반환
                return currentStageId;
            }
        }

        // 현재 웨이브 진행 수치를 반환
        public WaveProgress CurrentWaveProgress
        {
            get
            {
                // 웨이브 컨트롤러의 진행 수치를 반환
                return waveController.Progress;
            }
        }

        // Phase 상태 변경을 알리는 이벤트
        public event Action<PhaseChanged> PhaseChanged;

        // 웨이브 시작 요청을 알리는 이벤트
        public event Action<WaveStartRequested> WaveStartRequested;

        // 스테이지 완료를 알리는 이벤트
        public event Action<StageCompleted> StageCompleted;

        // 지정한 스테이지를 Preparation 상태로 생성
        public PhaseSystem(int stageId)
        {
            // 스테이지 번호가 음수인지 확인
            if (stageId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageId));
            }

            currentStageId = stageId;
            currentState = PhaseState.Preparation;
            waveController = new WaveController(stageId);
        }

        // Preparation에서 Combat으로 한 번 전환
        public bool TryStartCombat(StartCombatCommand command)
        {
            // Preparation 상태에서만 전투 시작을 허용
            if (currentState != PhaseState.Preparation)
            {
                // 중복 전투 시작 실패를 반환
                return false;
            }

            waveController.StartWave();
            // 상태 변경 전의 Phase 상태를 보관
            PhaseState previousState = currentState;
            currentState = PhaseState.Combat;
            PhaseChanged?.Invoke(new PhaseChanged(previousState, currentState));
            WaveStartRequested?.Invoke(new WaveStartRequested(currentStageId));

            // 전투 시작 성공을 반환
            return true;
        }

        // 현재 Combat 웨이브에 적을 등록
        public bool TryRegisterEnemySpawned(out EnemyStageToken enemyToken)
        {
            // 기본 실패 토큰을 먼저 준비
            enemyToken = default(EnemyStageToken);

            // Combat 상태에서만 적 등록을 허용
            if (currentState != PhaseState.Combat)
            {
                // 등록 실패를 반환
                return false;
            }

            // 웨이브 컨트롤러에 등록 결과를 반환
            return waveController.TryRegisterEnemySpawned(out enemyToken);
        }

        // 현재 Combat 웨이브의 적 처치를 보고
        public bool TryReportEnemyDefeated(EnemyStageToken enemyToken)
        {
            // Combat 상태에서만 처치 보고를 허용
            if (currentState != PhaseState.Combat)
            {
                // 처치 보고 실패를 반환
                return false;
            }

            // 웨이브 컨트롤러가 유효한 처치를 반영하는지 확인
            if (!waveController.TryReportEnemyDefeated(enemyToken))
            {
                // 처치 보고 실패를 반환
                return false;
            }

            // 처치 후 완료 조건을 확인
            TryPublishStageCompletion();

            // 처치 보고 성공을 반환
            return true;
        }

        // 현재 Combat 웨이브의 생성 종료를 보고
        public bool TryCompleteWaveSpawning()
        {
            // Combat 상태에서만 생성 종료를 허용
            if (currentState != PhaseState.Combat)
            {
                // 생성 종료 실패를 반환
                return false;
            }

            // 웨이브 컨트롤러가 생성 종료를 반영하는지 확인
            if (!waveController.TryCompleteWaveSpawning())
            {
                // 중복 생성 종료 실패를 반환
                return false;
            }

            // 생성 종료 후 완료 조건을 확인
            TryPublishStageCompletion();

            // 생성 종료 보고 성공을 반환
            return true;
        }

        // 완료된 웨이브를 StageClear로 한 번 전환
        private void TryPublishStageCompletion()
        {
            // 웨이브 완료와 이벤트 미발행 상태인지 확인
            if (!waveController.IsStageComplete || stageCompletedPublished)
            {
                // 중복 완료 발행을 차단
                return;
            }

            stageCompletedPublished = true;
            // 완료 이벤트에 전달할 최종 결과를 생성
            StageResult result = new StageResult(
                currentStageId,
                true,
                waveController.Progress);
            StageCompleted?.Invoke(new StageCompleted(result));
            // StageClear 전환 전의 Phase 상태를 보관
            PhaseState previousState = currentState;
            currentState = PhaseState.StageClear;
            PhaseChanged?.Invoke(new PhaseChanged(previousState, currentState));
        }
    }
}
