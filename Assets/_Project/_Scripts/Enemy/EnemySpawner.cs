using UnityEngine;
using System.Collections;
using EnemySystem; // [추가] Enemy 스크립트가 속한 네임스페이스 연결
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    // [추가] 외부(Enemy.cs 등)에서 스포너에 접근할 수 있도록 싱글톤 뼈대 배치
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Area Settings (3D Region)")]
    public float minXOffset = -2f;
    public float maxXOffset = -4f;
    public float spawnHeight = 10f;

    [Header("Physics Settings")]
    public LayerMask groundLayer;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;

    // [추가] 적 추적을 위한 카운팅 변수들
    private int totalEnemiesToSpawn = 0; // 이번 웨이브에 소환해야 할 총 마리수
    private int spawnedEnemyCount = 0;   // 현재까지 소환된 마리수
    private int activeEnemyCount = 0;    // 현재 필드에 살아있는 마리수
    private bool isAllSpawned = false;   // 소환이 모두 끝났는지 여부

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartSpawning(GameObject prefab, int count, float interval)
    {
        StopAllCoroutines();

        // [추가] 새로운 소환 명령이 내려올 때 카운팅 변수들 초기화
        totalEnemiesToSpawn = count;
        spawnedEnemyCount = 0;
        activeEnemyCount = 0;
        isAllSpawned = false;

        StartCoroutine(SpawnRoutine(prefab, count, interval));
    }

    private IEnumerator SpawnRoutine(GameObject prefab, int count, float interval)
    {
        Debug.Log($"[Spawner] 소환 시작: {count}마리");

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionIn3DRegion();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            // [추가] 적이 생성될 때 카운트 증가 및 스포너 등록
            spawnedEnemyCount++;
            activeEnemyCount++;

            Enemy enemyScript = enemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetSpawner(this); // 적에게 "내가 너희 스포너야"라고 알려줌
            }

            // EnemyController가 있다면 활성화 (기존 로직 유지)
            if (enemy.TryGetComponent(out EnemyController controller))
            {
                controller.ActivateEnemy();
            }

            yield return new WaitForSeconds(interval);
        }

        // [추가] 루프가 끝나면 소환 자체는 완전히 끝났다고 플래그를 세웁니다.
        isAllSpawned = true;
        Debug.Log("[Spawner] 모든 적 생성 완료. 남은 적들을 소탕해야 합니다.");
    }

    /// <summary>
    /// [추가] 적이 죽을 때 (Enemy.cs의 Die() 함수에서) 실시간으로 호출해 줄 함수
    /// </summary>
    public void OnEnemyDestroyed()
    {
        activeEnemyCount--;
        Debug.Log($"<color=green>[Spawner]</color> 적 처치됨! 필드에 남은 적: {activeEnemyCount} / {totalEnemiesToSpawn}");

        // [핵심 체크] 데이터상으로 모든 소환이 끝났고, 필드에 남은 고블린/아처가 0마리라면?
        if (isAllSpawned && activeEnemyCount <= 0)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        Debug.Log("<color=yellow>[Spawner] 필드의 모든 적이 소멸되었습니다! 스테이지 클리어!</color>");

        // GameEndManager를 깨워서 스테이지 클리어 시퀀스 발동!
        GameEndManager.Instance.TriggerGameEnd(GameEndReason.StageCleared);
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

    //테스트용 적 전부 죽이는 함수
    private void Update()
    {
        if(Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillAllEnemiesCheat();
        }
    }

    private void KillAllEnemiesCheat()
    {
        // 씬에 존재하는 모든 Enemy(부모 클래스)를 찾아서 배열로 가져옵니다.
        EnemySystem.Enemy[] allEnemies = FindObjectsByType<EnemySystem.Enemy>(FindObjectsSortMode.None);

        if (allEnemies.Length == 0)
        {
            Debug.Log("<color=yellow>[Cheat]</color> 필드에 죽일 적이 없습니다.");
            return;
        }

        Debug.Log($"<color=red>[Cheat]</color> 치트키 발동! 필드의 모든 적({allEnemies.Length}마리)을 처치합니다.");

        // 찾은 적들을 하나씩 순회하며 Die() 함수를 강제로 실행시킵니다.
        foreach (EnemySystem.Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }
}