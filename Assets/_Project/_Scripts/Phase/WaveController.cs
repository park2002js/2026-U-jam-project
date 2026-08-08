using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    public sealed class WaveController : MonoBehaviour
    {
        // 현재 Scene에서 사용할 단일 WaveController
        public static WaveController Instance { get; private set; }

        // 순서대로 진행할 전체 Wave 정보
        [SerializeField] private WaveInfo[] _waves;

        // 활성화된 Enemy를 정리할 선택적 부모 (생성된 적들을 Hierarchy에서 한곳에 모아 관리하기 위한 선택적 부모)
        [SerializeField] private Transform _activeEnemyRoot;

        // 현재 Wave에 속한 생존 Enemy 식별자
        private readonly HashSet<int> _aliveEnemyIds = new HashSet<int>();

        // 남은 수와 완료를 전달할 PhaseSystem
        private PhaseSystem _phaseSystem;

        // 발표용 Enemy에 주입할 기본 거점 Target
        private GameObject _defaultEnemyTarget;

        // 현재 진행 중인 Wave 배열 위치
        private int _currentWaveIndex = -1;

        // 현재 Wave에서 남은 Enemy 수
        private int _remainingEnemyCount;

        // 현재 Wave에서 죽은 Enemy 수
        private int _deadEnemyCount;

        // Wave 준비 또는 전투가 진행 중인지 여부
        private bool _isWaveRunning;

        // Singleton 초기화
        private void Awake()
        {
            // 이미 다른 WaveController가 등록됐는지 확인
            if (Instance != null && Instance != this)
            {
                enabled = false;

                // 중복 Singleton 초기화 중단
                return;
            }

            Instance = this;
        }

        // Singleton 정리
        private void OnDestroy()
        {
            // 연결된 Phase 변경 로그 해제
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged -= HandlePhaseChanged;
            }

            // 자신이 등록한 Singleton만 해제
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 남은 수와 Wave 완료를 받을 PhaseSystem 연결
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            // 기존 Phase 변경 로그 해제
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged -= HandlePhaseChanged;
            }

            _phaseSystem = phaseSystem;

            // 새 Phase 변경 로그 연결
            if (_phaseSystem != null)
            {
                _phaseSystem.PhaseChanged += HandlePhaseChanged;
                Debug.Log($"[WaveController] Phase 시작: {_phaseSystem.CurrentState}", this);
            }
        }

        // GameManager가 발표용 기본 거점 Target 연결
        public void ConfigureDefaultTarget(GameObject target)
        {
            // 이후 생성할 Enemy에 전달할 거점 저장
            _defaultEnemyTarget = target;
        }

        // 다음 Wave의 총 Enemy 수 조회
        public int GetNextWaveEnemyCount()
        {
            // 다음 배열 위치 계산
            int nextWaveIndex = _currentWaveIndex + 1;

            // 다음 Wave가 없거나 비어 있는지 확인
            if (_waves == null
                || nextWaveIndex < 0
                || nextWaveIndex >= _waves.Length
                || _waves[nextWaveIndex] == null)
            {
                // 진행할 Wave가 없음을 반환
                return 0;
            }

            // 다음 Wave의 배열 길이 반환
            return _waves[nextWaveIndex].TotalEnemyCount;
        }

        // 기존 PhaseSystem 호출을 유지하는 호환용 진입점
        public bool StartNextWave()
        {
            int previousWaveIndex = _currentWaveIndex;
            WaveStart();
            return _currentWaveIndex != previousWaveIndex;
        }

        // 모든 Enemy를 한 Frame에 생성하고 개별 Move 대기를 시작
        public void WaveStart()
        {
            if (_isWaveRunning)
            {
                Debug.LogWarning("[WaveController] Wave 시작 실패: 이미 Wave가 진행 중임", this);
                return;
            }

            if (!TryGetValidNextWave(out WaveInfo nextWave)) return;

            _isWaveRunning = true;
            _currentWaveIndex += 1;
            _aliveEnemyIds.Clear();
            _remainingEnemyCount = nextWave.TotalEnemyCount;
            _deadEnemyCount = 0;

            if (_phaseSystem != null)
            {
                _phaseSystem.UpdateRemainingEnemyCount(_remainingEnemyCount);
                _phaseSystem.UpdateDeadEnemyCount(_deadEnemyCount);
            }

            foreach (WaveInfo.EnemySpawnInfo enemyInfo in nextWave.Enemies)
            {
                GameObject instance = Instantiate(
                    enemyInfo.EnemyPrefab,
                    GetWorldPosition(enemyInfo.GridPosition),
                    Quaternion.identity,
                    _activeEnemyRoot);
                EnemyBase enemy = instance.GetComponent<EnemyBase>();

                enemy.FSM.Targets.Clear();
                enemy.FSM.Targets.Add(_defaultEnemyTarget);
                _aliveEnemyIds.Add(instance.GetInstanceID());
                StartCoroutine(SetMoveAfterWait(enemy, enemyInfo.WaitTime));
            }

            Debug.Log($"[WaveController] Wave {_currentWaveIndex + 1} 시작: Enemy {_remainingEnemyCount}명", this);
        }

        // Enemy 사망을 중복 없이 반영하고 PhaseSystem에 보고
        public bool ReportEnemyDead(GameObject enemy)
        {
            // 사망한 Enemy가 없으면 보고를 거부
            if (enemy == null)
            {
                // 잘못된 사망 보고 실패 반환
                return false;
            }

            // 현재 Enemy의 Unity 식별자
            int enemyId = enemy.GetInstanceID();
            // 진행 중인 Wave 소속의 첫 사망인지 확인
            if (!_isWaveRunning || !_aliveEnemyIds.Remove(enemyId))
            {
                return false;
            }

            _remainingEnemyCount -= 1;
            _deadEnemyCount += 1;
            bool isWaveComplete = _remainingEnemyCount == 0;

            if (isWaveComplete) _isWaveRunning = false;

            Debug.Assert(_remainingEnemyCount >= 0, "Wave enemy count became negative.");

            // 최신 남은 수와 죽은 수를 PhaseSystem에 전달
            if (_phaseSystem != null)
            {
                _phaseSystem.UpdateRemainingEnemyCount(_remainingEnemyCount);
                _phaseSystem.UpdateDeadEnemyCount(_deadEnemyCount);
            }

            // 마지막 Enemy 사망인지 확인
            if (isWaveComplete && _phaseSystem != null)
            {
                _phaseSystem.CompleteCombatPhase();
            }

            // 사망 보고 성공 반환
            return true;
        }

        // 지정된 시간 뒤 생성된 Enemy를 Move 상태로 전환
        private IEnumerator SetMoveAfterWait(EnemyBase enemy, float waitTime)
        {
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            if (enemy != null) enemy.FSM.SetState(EnemyStateType.Move);
        }

        // 다음 Wave와 모든 Spawn 값 검증
        private bool TryGetValidNextWave(out WaveInfo wave)
        {
            // 실패 시 사용할 빈 Wave
            wave = null;
            // 다음 배열 위치
            int nextWaveIndex = _currentWaveIndex + 1;

            // 배열과 다음 Wave 존재 여부 확인
            if (_waves == null || nextWaveIndex < 0 || nextWaveIndex >= _waves.Length)
            {
                Debug.LogWarning(
                    $"[WaveController] Wave 시작 실패: 다음 Wave가 없음 (index: {nextWaveIndex})",
                    this);

                // 다음 Wave 없음 반환
                return false;
            }

            wave = _waves[nextWaveIndex];

            // Wave Asset 연결 여부 확인
            if (wave == null)
            {
                Debug.LogWarning(
                    $"[WaveController] Wave 시작 실패: Waves[{nextWaveIndex}]에 WaveInfo가 연결되지 않음",
                    this);

                // Wave 검증 실패 반환
                return false;
            }

            // Wave Enemy 존재 여부 확인
            if (wave.TotalEnemyCount == 0)
            {
                Debug.LogWarning(
                    $"[WaveController] Wave 시작 실패: Waves[{nextWaveIndex}]의 Enemy가 0명임",
                    this);
                wave = null;

                // Wave 검증 실패 반환
                return false;
            }

            // Grid 초기화 여부 확인
            if (!GridSystem.Instance.IsInitialized)
            {
                Debug.LogWarning("[WaveController] Wave 시작 실패: GridSystem이 초기화되지 않음", this);
                wave = null;

                // Wave 검증 실패 반환
                return false;
            }

            // Enemy 기본 Target 연결 여부 확인
            if (_defaultEnemyTarget == null)
            {
                Debug.LogWarning(
                    "[WaveController] Wave 시작 실패: GameManager의 BaseCore 참조가 비어 있음",
                    this);
                wave = null;

                // Wave 검증 실패 반환
                return false;
            }

            // 검증할 전체 Enemy 정보
            WaveInfo.EnemySpawnInfo[] enemies = wave.Enemies;

            // 모든 Enemy 정보의 필수 값 검사
            for (int index = 0; index < enemies.Length; index += 1)
            {
                // 현재 검사할 Enemy 정보
                WaveInfo.EnemySpawnInfo enemyInfo = enemies[index];
                // 현재 Enemy의 Grid 좌표
                Vector2Int gridPosition = enemyInfo.GridPosition;

                // Prefab과 Grid 범위와 대기시간 확인
                if (enemyInfo.EnemyPrefab == null
                    || enemyInfo.EnemyPrefab.GetComponent<EnemyBase>() == null
                    || gridPosition.x < 0
                    || gridPosition.x >= GridSystem.Instance.ColumnCount
                    || gridPosition.y < 0
                    || gridPosition.y >= GridSystem.Instance.RowCount
                    || enemyInfo.WaitTime < 0f
                    || float.IsNaN(enemyInfo.WaitTime)
                    || float.IsInfinity(enemyInfo.WaitTime))
                {
                    Debug.LogWarning(
                        $"[WaveController] Wave 시작 실패: Waves[{nextWaveIndex}] Enemy[{index}] 설정 오류 "
                        + $"(Prefab: {enemyInfo.EnemyPrefab}, Grid: {gridPosition}, WaitTime: {enemyInfo.WaitTime})",
                        this);
                    wave = null;

                    // 잘못된 Enemy 정보 실패 반환
                    return false;
                }
            }

            // 전체 Wave 정보 검증 성공 반환
            return true;
        }

        // PhaseSystem의 Phase 시작 로그 출력
        private void HandlePhaseChanged(PhaseState phase)
        {
            Debug.Log($"[WaveController] Phase 시작: {phase}", this);
        }

        // Grid 좌표를 기존 Grid 원점 규칙의 World 좌표로 변환
        private static Vector3 GetWorldPosition(Vector2Int gridPosition)
        {
            // 현재 Grid 정보 허브
            GridSystem grid = GridSystem.Instance;
            // col을 반영한 World x 좌표
            float worldX = grid.Origin.x + gridPosition.x * grid.CellWidth;
            // row를 반영한 World z 좌표
            float worldZ = grid.Origin.z + gridPosition.y * grid.CellHeight;

            // Grid 원점 높이를 유지한 World 좌표 반환
            return new Vector3(worldX, grid.Origin.y, worldZ);
        }
    }
}
