using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // [필수] 새로운 인풋 시스템 사용을 위한 네임스페이스
using EnemySystem;             // [필수] Enemy 스크립트가 속한 네임스페이스 연결

public class EnemySpawner : MonoBehaviour
{
    // 외부(Enemy.cs 등)에서 스포너에 접근할 수 있도록 하는 싱글톤 인스턴스
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Area Settings (3D Region)")]
    public float minXOffset = -2f;
    public float maxXOffset = -4f;
    public float spawnHeight = 10f;

    [Header("Physics Settings")]
    public LayerMask groundLayer;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;

    [Header("Wave Tracking (디버깅용 정보)")]
    [SerializeField] private int totalEnemiesToSpawn = 0; // 이번 웨이브 총 적 수
    [SerializeField] private int spawnedEnemyCount = 0;   // 현재까지 소환된 적 수
    [SerializeField] private int activeEnemyCount = 0;    // 현재 필드에 살아있는 적 수
    [SerializeField] private bool isAllSpawned = false;   // 소환 종료 여부

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // [치트키] 새로운 인풋 시스템 방식으로 K 키를 누르면 필드의 모든 적 처치
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillAllEnemiesCheat();
        }
    }

    /// <summary>
    /// 외부(WaveEntry 등)에서 새로운 웨이브/소환을 명령할 때 호출하는 함수
    /// </summary>
    public void StartSpawning(GameObject prefab, int count, float interval)
    {
        // 이전 소환 루틴이 돌고 있다면 중복 방지를 위해 정지
        StopAllCoroutines();

        // 새로운 소환에 맞춰 카운팅 변수들 초기화
        totalEnemiesToSpawn = count;
        spawnedEnemyCount = 0;
        activeEnemyCount = 0;
        isAllSpawned = false;

        // 2. 에러 수정: 옛날 함수 대신 비동기 소환 루틴을 실행합니다.
        StartCoroutine(PreSpawnAllRoutine());
    }

    private IEnumerator SpawnRoutine(GameObject prefab, int count, float interval)
    {
        Debug.Log($"<color=cyan>[Spawner]</color> 소환 시작: 총 {count}마리");

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionIn3DRegion();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            // 적이 생성될 때 카운트 증가
            spawnedEnemyCount++;
            activeEnemyCount++;

            // 생성된 적에게 이 스포너(나 자신)를 등록하여 추적 가능하게 만듦
            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetSpawner(this);
            }

            // EnemyController가 있다면 활성화 (기존 로직 유지)
            if (enemy.TryGetComponent(out EnemyController controller))
            {
                controller.ActivateEnemy();
            }

            yield return new WaitForSeconds(interval);
        }

        // 루프가 끝나면 소환 자체는 완전히 끝났다고 플래그를 세움
        isAllSpawned = true;
        Debug.Log("<color=cyan>[Spawner]</color> 모든 적 생성 완료. 남은 적들을 소탕해야 합니다.");
    }

    /// <summary>
    /// 적이 죽을 때 (Enemy.cs의 Die() 함수 안에서) 실시간으로 호출해 줄 함수
    /// </summary>
    public void OnEnemyDestroyed()
    {
        activeEnemyCount--;
        Debug.Log($"<color=green>[Spawner]</color> 적 처치됨! 필드에 남은 적: {activeEnemyCount} / {totalEnemiesToSpawn}");

        // 모든 소환이 끝났고, 필드에 남은 적이 0마리라면 스테이지 클리어!
        if (isAllSpawned && activeEnemyCount <= 0)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        Debug.Log("<color=yellow>[Spawner] 필드의 모든 적이 소멸되었습니다! 스테이지 클리어 연출 시작.</color>");

        // GameEndManager를 깨워서 스테이지 클리어 전역 시퀀스 발동
        if (GameEndManager.Instance != null)
        {
            GameEndManager.Instance.TriggerGameEnd(GameEndReason.StageCleared);
        }
        else
        {
            Debug.LogError("[Spawner] 하이라키 창에 GameEndManager 오브젝트가 없습니다!");
        }
    }

    /// <summary>
    /// [디버깅 치트키] K 키 입력 시 필드의 모든 적에게 Die()를 강제 수행하는 함수
    /// </summary>
    private void KillAllEnemiesCheat()
    {
        // 씬에 존재하는 모든 Enemy(부모 클래스)를 찾아서 배열로 가져옴
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (allEnemies.Length == 0)
        {
            Debug.Log("<color=yellow>[Cheat]</color> 필드에 죽일 적이 없습니다.");
            return;
        }

        Debug.Log($"<color=red>[Cheat]</color> 치트키 발동! 필드의 모든 적({allEnemies.Length}마리)을 처치합니다.");

        // 순회하며 안전하게 Die() 호출 -> 스포너 카운트 정상 감소 유도
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }

    private Vector3 GetRandomPositionIn3DRegion()
    {
        Vector3 originCenter = transform.position;

        float randomX = Random.Range(minXOffset, maxXOffset);
        float randomZ = Random.Range(-spawnHeight / 2f, spawnHeight / 2f);

        Vector3 rayOrigin = originCenter + new Vector3(randomX, 10f, randomZ);
        Vector3 finalPos = rayOrigin;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            finalPos = hit.point;
        }
        else
        {
            finalPos.y = originCenter.y;
        }

        return finalPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        float centerXOffset = (minXOffset + maxXOffset) / 2f;
        Vector3 spawnCenter = transform.position + new Vector3(centerXOffset, 0f, 0f);
        float spawnWidth = Mathf.Abs(maxXOffset - minXOffset);

        Gizmos.DrawWireCube(spawnCenter, new Vector3(spawnWidth, 1f, spawnHeight));
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(spawnCenter, new Vector3(spawnWidth, 1f, spawnHeight));
    }
}