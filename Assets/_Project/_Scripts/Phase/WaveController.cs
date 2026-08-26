using System.Collections;
using System.Collections.Generic;
using System.IO;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Phase
{
    public class WaveController : MonoBehaviour
    {
        // JSON의 Prefab 이름과 실제 Enemy Prefab을 연결하기 위한 정보
        [System.Serializable]
        private struct EnemyPrefabEntry
        {
            [SerializeField] private string _name;
            [SerializeField] private GameObject _prefab;

            // JSON에서 사용할 Prefab 이름
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            // 이름과 연결된 실제 Enemy Prefab
            public GameObject Prefab
            {
                get
                {
                    return _prefab;
                }
            }
        }

        // 현재 Scene에서 사용할 단일 WaveController
        public static WaveController Instance { get; private set; }

        // 순서대로 진행할 전체 Wave 정보
        [SerializeField] private WaveInfo[] _waves;
        
        // JSON에 적힌 Prefab 이름과 실제 Enemy Prefab 연결 정보
        [SerializeField] private EnemyPrefabEntry[] _enemyPrefabs;

        // 활성화된 Enemy를 정리할 선택적 부모 (생성된 적들을 Hierarchy에서 한곳에 모아 관리하기 위한 선택적 부모)
        [SerializeField] private Transform _activeEnemyRoot;

        // 현재 Wave에 속한 생존 Enemy 식별자
        private readonly HashSet<int> _aliveEnemyIds = new HashSet<int>();

        // 남은 수와 완료를 전달할 PhaseSystem
        private PhaseSystem _phaseSystem;
        private GameManager _gameManager;

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

             // JSON 파일에서 Wave 정보 불러오기
            LoadWaveData();
        }

        // Singleton 정리
        private void OnDestroy()
        {
            // 연결된 Phase 변경 로그 해제
            if (_gameManager != null)
            {
                _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            }

            // 자신이 등록한 Singleton만 해제
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// 적 수와 완료를 보고할 PhaseSystem을 연결하고, Phase 알림은 GameManager.Instance에서 구독합니다.
        /// </summary>
        public void ConfigurePhaseSystem(PhaseSystem phaseSystem)
        {
            // 기존 Phase 변경 로그 해제
            if (_gameManager != null)
            {
                _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            }

            _phaseSystem = phaseSystem;
            _gameManager = GameManager.Instance;

            // 새 Phase 변경 로그 연결
            if (_phaseSystem != null && _gameManager != null)
            {
                _gameManager.OnPhaseChanged += HandlePhaseChanged;
                Debug.Log("[WaveController] PhaseSystem 연결 완료", this);
            }
        }

        // GameManager가 발표용 기본 거점 Target 연결
        public void ConfigureDefaultTarget(GameObject target)
        {
            // 이후 생성할 Enemy에 전달할 거점 저장
            _defaultEnemyTarget = target;
        }
        
        // JSON 파일을 읽어 전체 Wave 정보를 불러옴
        private void LoadWaveData()
        {
            // Prefab 이름과 실제 Prefab을 연결할 Dictionary 생성
            Dictionary<string, GameObject> prefabDictionary =
                new Dictionary<string, GameObject>();

            // Inspector에 등록된 Prefab 정보를 Dictionary로 변환
            foreach (EnemyPrefabEntry entry in _enemyPrefabs ?? System.Array.Empty<EnemyPrefabEntry>())
            {
                // 이름이 비어 있거나 Prefab이 없으면 제외
                if (string.IsNullOrWhiteSpace(entry.Name)
                    || entry.Prefab == null)
                {
                    continue;
                }

                // 예: "Goblin" -> Goblin Prefab
                prefabDictionary[entry.Name] = entry.Prefab;
            }

            // JSON 파일을 읽을 WaveInfoFileReader 생성
            WaveInfoFileReader reader =
                new WaveInfoFileReader(prefabDictionary);

            // Wave JSON 파일들이 저장된 폴더 경로
            string directoryPath =
                Path.Combine(
                    Application.streamingAssetsPath,
                    "Waves");

            // JSON 파일들을 읽어 WaveInfo 배열로 변환
            _waves = reader.LoadWaveData(directoryPath);

            Debug.Log(
                $"[WaveController] Wave {_waves.Length}개 로드 완료",
                this);
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

        // 생성 대기 중인 적까지 남은 수에 포함하고 각각 Wave 시작 기준의 지연 생성을 예약한다.
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
                StartCoroutine(SpawnAfterWait(enemyInfo));
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

        // waitTime은 이전 적이 생성된 시점이 아니라 Wave 시작 시점부터 센다.
        private IEnumerator SpawnAfterWait(WaveInfo.EnemySpawnInfo enemyInfo)
        {
            if (enemyInfo.WaitTime > 0f) yield return new WaitForSeconds(enemyInfo.WaitTime);
            if (!_isWaveRunning || (_gameManager != null && _gameManager.IsGameOver)) yield break;

            GameObject instance = Instantiate(enemyInfo.EnemyPrefab, GetWorldPosition(enemyInfo.GridPosition), enemyInfo.EnemyPrefab.transform.rotation, _activeEnemyRoot);
            EnemyBase enemy = instance.GetComponent<EnemyBase>();
            enemy.FSM.Targets.Clear();
            enemy.FSM.Targets.Add(_defaultEnemyTarget);
            _aliveEnemyIds.Add(instance.GetInstanceID());
            enemy.FSM.SetState(EnemyStateType.Move);
            Debug.Log($"[WaveController] {enemyInfo.EnemyPrefab.name} 생성: Grid {enemyInfo.GridPosition}, 지연 {enemyInfo.WaitTime}초", this);
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
            if (GridSystem.Instance == null || !GridSystem.Instance.IsInitialized)
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
                    || !enemyInfo.EnemyPrefab.activeSelf
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

        /// <summary>
        /// GameManager가 중계한 Phase 변경을 로그로 표시합니다.
        /// </summary>
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

            // 적은 Grid 원점 높이와 무관하게 바닥에서 0.1만큼 띄워 생성한다.
            return new Vector3(worldX, 0.1f, worldZ);
        }
    }

    
}
