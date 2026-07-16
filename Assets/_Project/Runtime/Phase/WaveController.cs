using System;
using System.Collections.Generic;

namespace UJam.Runtime.Phase
{
    public sealed class WaveController : IStageEnemyReporter
    {
        // 이 웨이브가 담당하는 스테이지 번호를 보관
        private readonly int stageId;

        // 아직 처치되지 않은 토큰을 보관
        private readonly HashSet<EnemyStageToken> aliveTokens = new HashSet<EnemyStageToken>();

        // 이미 처치된 토큰을 보관
        private readonly HashSet<EnemyStageToken> defeatedTokens = new HashSet<EnemyStageToken>();

        // 다음 토큰에 사용할 순번을 보관
        private long nextSequence;

        // 웨이브 시작 여부를 보관
        private bool isWaveStarted;

        // 웨이브 생성 종료 여부를 보관
        private bool isSpawningComplete;

        // 스테이지 완료 여부를 보관
        private bool isStageComplete;

        // 현재 웨이브 진행 수치를 보관
        private WaveProgress waveProgress;

        // 웨이브의 스테이지 번호를 반환
        public int StageId
        {
            get
            {
                // 저장된 스테이지 번호를 반환
                return stageId;
            }
        }

        // 현재 웨이브 진행 수치를 반환
        public WaveProgress Progress
        {
            get
            {
                // 저장된 진행 수치를 반환
                return waveProgress;
            }
        }

        // 웨이브 생성 종료 여부를 반환
        public bool IsSpawningComplete
        {
            get
            {
                // 생성 종료 상태를 반환
                return isSpawningComplete;
            }
        }

        // 스테이지 완료 여부를 반환
        public bool IsStageComplete
        {
            get
            {
                // 스테이지 완료 상태를 반환
                return isStageComplete;
            }
        }

        // 지정한 스테이지의 빈 웨이브를 준비
        public WaveController(int stageId)
        {
            // 스테이지 번호가 음수인지 확인
            if (stageId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stageId));
            }

            this.stageId = stageId;
            waveProgress = new WaveProgress(0, 0, 0);
        }

        // 웨이브를 시작하고 이전 진행 상태를 초기화
        public void StartWave()
        {
            // 이미 시작한 웨이브는 상태를 유지
            if (isWaveStarted)
            {
                // 이미 시작된 웨이브의 중복 초기화를 차단
                return;
            }

            // 첫 시작 상태를 기록
            isWaveStarted = true;
            // 새 웨이브의 생성 종료 상태를 초기화
            isSpawningComplete = false;
            isStageComplete = false;
            nextSequence = 0;
            aliveTokens.Clear();
            defeatedTokens.Clear();
            waveProgress = new WaveProgress(0, 0, 0);
        }

        // 현재 웨이브에 적 하나를 등록하고 토큰을 발행
        public bool TryRegisterEnemySpawned(out EnemyStageToken enemyToken)
        {
            // 기본 실패 토큰을 먼저 준비
            enemyToken = default(EnemyStageToken);

            // 시작 전이나 생성 종료 뒤 등록을 차단
            if (!isWaveStarted || isSpawningComplete || isStageComplete)
            {
                // 등록 실패를 반환
                return false;
            }

            // 다음 순번을 안전하게 증가
            if (nextSequence == long.MaxValue)
            {
                // 토큰 순번 고갈 시 등록 실패를 반환
                return false;
            }

            nextSequence++;
            enemyToken = new EnemyStageToken(stageId, nextSequence);
            aliveTokens.Add(enemyToken);
            // 등록 후 갱신할 전체 생성 수치를 계산
            int totalSpawned = waveProgress.TotalSpawned + 1;
            // 등록 후 갱신할 생존 수치를 계산
            int alive = waveProgress.Alive + 1;
            // 등록 전 처치 수치를 유지
            int defeated = waveProgress.Defeated;
            waveProgress = new WaveProgress(
                totalSpawned,
                alive,
                defeated);

            // 등록 성공을 반환
            return true;
        }

        // 유효한 토큰 하나의 처치를 반영
        public bool TryReportEnemyDefeated(EnemyStageToken enemyToken)
        {
            // 처치 토큰이 유효한지 확인
            if (!enemyToken.IsValid || enemyToken.StageId != stageId)
            {
                // 잘못된 토큰의 처치 실패를 반환
                return false;
            }

            // 아직 살아 있는 등록 토큰인지 확인
            if (defeatedTokens.Contains(enemyToken) || !aliveTokens.Remove(enemyToken))
            {
                // 미등록 또는 중복 처치 실패를 반환
                return false;
            }

            defeatedTokens.Add(enemyToken);
            // 처치 후에도 유지할 전체 생성 수치를 보관
            int totalSpawned = waveProgress.TotalSpawned;
            // 처치 후 감소할 생존 수치를 계산
            int alive = waveProgress.Alive - 1;
            // 처치 후 증가할 처치 수치를 계산
            int defeated = waveProgress.Defeated + 1;
            waveProgress = new WaveProgress(
                totalSpawned,
                alive,
                defeated);
            TryCompleteStageIfReady();

            // 처치 반영 성공을 반환
            return true;
        }

        // 웨이브 생성 종료를 한 번 반영
        public bool TryCompleteWaveSpawning()
        {
            // 시작 전이나 이미 종료된 웨이브의 완료 보고를 차단
            if (!isWaveStarted || isSpawningComplete || isStageComplete)
            {
                // 중복 완료 실패를 반환
                return false;
            }

            isSpawningComplete = true;
            TryCompleteStageIfReady();

            // 생성 종료 반영 성공을 반환
            return true;
        }

        // 생성 종료와 생존 적 소진 여부를 확인해 스테이지를 완료
        private void TryCompleteStageIfReady()
        {
            // 생성이 끝나고 살아 있는 적이 없는지 확인
            if (!isSpawningComplete || waveProgress.Alive != 0)
            {
                // 아직 완료할 수 없어 상태를 유지
                return;
            }

            // 스테이지 완료를 한 번만 기록
            isStageComplete = true;
        }
    }
}
