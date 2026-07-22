using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    public sealed class WaveController : MonoBehaviour
    {
        // 준비된 Enemy 객체와 개별 대기시간
        private readonly struct PreparedEnemy
        {
            // 비활성 상태로 준비된 Enemy 객체
            public GameObject Instance { get; }

            // 활성화 전 대기시간
            public float WaitTime { get; }

            // 준비된 Enemy 객체와 대기시간 저장
            public PreparedEnemy(GameObject instance, float waitTime)
            {
                Instance = instance;
                WaitTime = waitTime;
            }
        }

        // 현재 Scene에서 사용할 단일 WaveController
        public static WaveController Instance { get; private set; }

        // 순서대로 진행할 전체 Wave 정보
        [SerializeField] private WaveInfo[] _waves;

        // 활성화된 Enemy를 정리할 선택적 부모
        [SerializeField] private Transform _activeEnemyRoot;

        // 비동기 생성이 끝난 Enemy와 대기시간 목록
        private readonly List<PreparedEnemy> _preparedEnemies = new List<PreparedEnemy>();

        // 현재 Wave에 속한 생존 Enemy 식별자
        private readonly HashSet<int> _aliveEnemyIds = new HashSet<int>();

        // 중복 사망과 남은 수 갱신을 한 번에 보호할 잠금 객체
        private readonly object _enemyCountLock = new object();

        // 준비 중인 Enemy를 비활성 상태로 유지할 런타임 부모
        private Transform _preparedRoot;

        // 남은 수와 완료를 전달할 PhaseSystem
        private PhaseSystem _phaseSystem;

        // 현재 진행 중인 Wave 배열 위치
        private int _currentWaveIndex = -1;

        // 현재 Wave에서 남은 Enemy 수
        private int _remainingEnemyCount;

        // Wave 준비 또는 전투가 진행 중인지 여부
        private bool _isWaveRunning;

        // Singleton과 비활성 준비 루트 초기화
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

            // 비동기 생성 결과를 숨길 런타임 객체 생성
            GameObject preparedRootObject = new GameObject("Prepared Enemies");
            preparedRootObject.SetActive(false);
            _preparedRoot = preparedRootObject.transform;
        }

        // Singleton과 런타임 준비 객체 정리
        private void OnDestroy()
        {
            // 자신이 등록한 Singleton만 해제
            if (Instance == this)
            {
                Instance = null;
            }

            // 생성한 준비 루트가 남아 있는지 확인
            if (_preparedRoot != null)
            {
                Destroy(_preparedRoot.gameObject);
            }
        }

        // 남은 수와 Wave 완료를 받을 PhaseSystem 연결
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            _phaseSystem = phaseSystem;
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

        // 다음 Wave를 검증하고 비동기 준비와 활성화 시작
        public bool StartNextWave()
        {
            // 검증된 다음 Wave 정보
            WaveInfo nextWave;

            // 진행 중이거나 다음 Wave가 잘못됐는지 확인
            if (_isWaveRunning || !TryGetValidNextWave(out nextWave))
            {
                // Wave 시작 실패 반환
                return false;
            }

            _isWaveRunning = true;
            _currentWaveIndex += 1;
            _preparedEnemies.Clear();

            // 새 Wave의 사망 장부와 남은 수를 함께 초기화
            lock (_enemyCountLock)
            {
                _aliveEnemyIds.Clear();
                _remainingEnemyCount = nextWave.TotalEnemyCount;
            }

            // PhaseSystem에 최초 남은 수 전달
            if (_phaseSystem != null)
            {
                _phaseSystem.UpdateRemainingEnemyCount(_remainingEnemyCount);
            }

            StartCoroutine(PrepareAndActivateWave(nextWave));

            // Wave 시작 성공 반환
            return true;
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
            // 잠금 밖에서 전달할 남은 수
            int remainingEnemyCount;
            // 잠금 밖에서 전달할 Wave 완료 여부
            bool isWaveComplete;

            // 소속 확인과 수치 변경을 하나의 원자적 구간으로 처리
            lock (_enemyCountLock)
            {
                // 진행 중인 Wave 소속의 첫 사망인지 확인
                if (!_isWaveRunning || !_aliveEnemyIds.Remove(enemyId))
                {
                    // 외부 Enemy 또는 중복 사망 보고 실패 반환
                    return false;
                }

                _remainingEnemyCount -= 1;
                remainingEnemyCount = _remainingEnemyCount;
                isWaveComplete = remainingEnemyCount == 0;

                // 마지막 Enemy 사망 시 다음 Wave 시작 가능 상태로 변경
                if (isWaveComplete)
                {
                    _isWaveRunning = false;
                }
            }

            // 원자적 감소 결과가 음수가 되지 않았는지 실행 중 확인
            Debug.Assert(remainingEnemyCount >= 0, "Wave enemy count became negative.");

            // 최신 남은 수를 PhaseSystem에 전달
            if (_phaseSystem != null)
            {
                _phaseSystem.UpdateRemainingEnemyCount(remainingEnemyCount);
            }

            // 마지막 Enemy 사망인지 확인
            if (isWaveComplete && _phaseSystem != null)
            {
                _phaseSystem.CompleteCombatPhase();
            }

            // 사망 보고 성공 반환
            return true;
        }

        // 모든 Enemy를 비동기로 준비한 뒤 대기를 한 번에 시작
        private IEnumerator PrepareAndActivateWave(WaveInfo wave)
        {
            // 현재 Wave의 모든 Enemy 정보
            WaveInfo.EnemySpawnInfo[] enemies = wave.Enemies;

            // Enemy를 배열 순서대로 비동기 생성
            for (int index = 0; index < enemies.Length; index += 1)
            {
                // 현재 생성할 Enemy 정보
                WaveInfo.EnemySpawnInfo enemyInfo = enemies[index];
                // Grid 좌표를 변환한 World 좌표
                Vector3 worldPosition = GetWorldPosition(enemyInfo.GridPosition);
                // Unity 6 비동기 Instantiate 작업
                AsyncInstantiateOperation<GameObject> operation = UnityEngine.Object.InstantiateAsync(
                    enemyInfo.EnemyPrefab,
                    1,
                    _preparedRoot,
                    worldPosition,
                    Quaternion.identity);

                // 현재 Enemy 준비 완료까지 Coroutine 양보
                yield return operation;

                // 비활성 준비 루트 아래에 생성된 Enemy
                GameObject enemy = operation.Result[0];
                // Enemy 객체와 개별 대기시간 묶음
                PreparedEnemy preparedEnemy = new PreparedEnemy(enemy, enemyInfo.WaitTime);
                _preparedEnemies.Add(preparedEnemy);

                // Wave 소속 Enemy 식별자 등록
                lock (_enemyCountLock)
                {
                    _aliveEnemyIds.Add(enemy.GetInstanceID());
                }
            }

            ActivatePreparedEnemies();
        }

        // 준비된 모든 Enemy의 개별 대기를 같은 Frame에 시작
        private void ActivatePreparedEnemies()
        {
            // 준비된 Enemy 전체를 동기 순회
            foreach (PreparedEnemy preparedEnemy in _preparedEnemies)
            {
                StartCoroutine(ActivateAfterWait(preparedEnemy));
            }

            _preparedEnemies.Clear();
        }

        // 지정된 시간 뒤 Enemy를 실제 Scene에 활성화
        private IEnumerator ActivateAfterWait(PreparedEnemy preparedEnemy)
        {
            // 양수 대기시간이 있는지 확인
            if (preparedEnemy.WaitTime > 0f)
            {
                // Enemy별 시간만큼 Coroutine 양보
                yield return new WaitForSeconds(preparedEnemy.WaitTime);
            }

            // 대기 중 Enemy가 제거됐는지 확인
            if (preparedEnemy.Instance == null)
            {
                // 제거된 Enemy 활성화 중단
                yield break;
            }

            preparedEnemy.Instance.transform.SetParent(_activeEnemyRoot, true);
            preparedEnemy.Instance.SetActive(true);
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
                // 다음 Wave 없음 반환
                return false;
            }

            wave = _waves[nextWaveIndex];

            // Wave와 Grid 준비 여부 확인
            if (wave == null || wave.TotalEnemyCount == 0 || !GridSystem.Instance.IsInitialized)
            {
                // 실행 불가능한 Wave 반환
                wave = null;

                // Wave 검증 실패 반환
                return false;
            }

            // 검증할 전체 Enemy 정보
            WaveInfo.EnemySpawnInfo[] enemies = wave.Enemies;

            // 모든 Enemy 정보의 필수 값 검사
            foreach (WaveInfo.EnemySpawnInfo enemyInfo in enemies)
            {
                // 현재 Enemy의 Grid 좌표
                Vector2Int gridPosition = enemyInfo.GridPosition;

                // Prefab과 Grid 범위와 대기시간 확인
                if (enemyInfo.EnemyPrefab == null
                    || gridPosition.x < 0
                    || gridPosition.x >= GridSystem.Instance.ColumnCount
                    || gridPosition.y < 0
                    || gridPosition.y >= GridSystem.Instance.RowCount
                    || enemyInfo.WaitTime < 0f
                    || float.IsNaN(enemyInfo.WaitTime)
                    || float.IsInfinity(enemyInfo.WaitTime))
                {
                    wave = null;

                    // 잘못된 Enemy 정보 실패 반환
                    return false;
                }
            }

            // 전체 Wave 정보 검증 성공 반환
            return true;
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
