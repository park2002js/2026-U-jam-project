using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using EnemySystem;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("스폰 설정")]
    [SerializeField] private Transform[] spawnPoints;

    private List<Enemy> activeEnemies = new List<Enemy>();

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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillAllEnemiesCheat();
        }
    }

    /// <summary>
    /// 옛날 단일 스폰 방식 유지보수용
    /// </summary>
    public void StartSpawning(GameObject prefab, int count, float interval)
    {
        StopAllAllCoroutinesBeforeNewWave(count);
        StartCoroutine(SpawnRoutine(prefab, count, interval));
    }

    private IEnumerator SpawnRoutine(GameObject prefab, int count, float interval)
    {
        Debug.Log($"<color=cyan>[Spawner]</color> 단일 소환 시작: 총 {count}마리");

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionIn3DRegion();
            SpawnSingleEnemy(prefab, spawnPosition);
            yield return new WaitForSeconds(interval);
        }

        isAllSpawned = true;
        Debug.Log("<color=cyan>[Spawner]</color> 모든 적 생성 완료.");
    }

    /// <summary>
    /// WaveEntry에서 웨이브 배치 명세서(WaveData)를 받아와 다중 소환을 처리하는 메인 함수
    /// </summary>
    public void StartWaveFromData(WaveData waveData)
    {
        if (waveData == null || waveData.spawnList == null || waveData.spawnList.Count == 0)
        {
            Debug.LogError("[EnemySpawner] 처리할 WaveData 내용이 비어있습니다");
            return;
        }

        // 1. 새로운 웨이브 사양에 맞춰 카운팅 변수들 완벽 초기화!
        int totalCount = waveData.GetTotalEnemyCount();
        StopAllAllCoroutinesBeforeNewWave(totalCount);

        // 2. 신형 배치 명세서 전용 코루틴 가동
        StartCoroutine(WaveSpawnRoutine(waveData));
    }

    private IEnumerator WaveSpawnRoutine(WaveData waveData)
    {
        Debug.Log("<color=yellow><b>[스포너]</b></color> 웨이브 명세서 지정 좌표 기반 소환 시작! 총 적 수: " + totalEnemiesToSpawn);

        foreach (SpawnInfo info in waveData.spawnList)
        {
            // 🚨 변수명 검사 매칭 (원래 이름인 enemyPrefab과 개별 리스트인 spawnOffset 예외처리 체크)
            if (info.enemyPrefab == null || info.spawnOffset == null || info.spawnOffset.Count == 0) continue;

            // 서브 웨이브 시작 전 기획적 딜레이 처리
            if (info.startDelay > 0f)
            {
                yield return new WaitForSeconds(info.startDelay);
            }

            // 좌표 개수만큼 루프 순회
            for (int i = 0; i < info.spawnOffset.Count; i++)
            {
                // 안전장치: 게임 오버 상태라면 즉시 중단
                if (GameEndManager.Instance != null && GameEndManager.Instance.IsGameEnded)
                {
                    Debug.LogError($"<color=red><b>[스포너 차단]</b></color> 게임 엔드 상태로 판단되어 소환을 취소합니다.");
                    yield break;
                }

                // 🎯 [교정 1] 스포너 월드 좌표에 i번째 개별 상대 오프셋 좌표를 더함
                Vector3 finalSpawnPosition = transform.position + info.spawnOffset[i];

                // 🎯 [교정 2] 공중에서 쏘는 레이캐스트 시작지점 y축 보정
                Vector3 rayOrigin = new Vector3(finalSpawnPosition.x, finalSpawnPosition.y + 10f, finalSpawnPosition.z);

                // 지형에 맞게 바닥으로 정확히 떨어뜨리기 (거리 인자 20f 추가 안전장치)
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    finalSpawnPosition = hit.point;
                }

                // 🎯 [교정 3] 계산한 고유 좌표(finalSpawnPosition)를 그대로 집어넣어 소환!
                SpawnSingleEnemy(info.enemyPrefab, finalSpawnPosition);

                yield return new WaitForSeconds(info.spawnInterval);
            }
        }

        // 3. 모든 명세서 연산이 완벽하게 끝났을 때만 승리 자격 플래그 On!
        isAllSpawned = true;
        Debug.Log("<color=yellow><b>[스포너 완료]</b></color> 이번 웨이브 명세서의 모든 적 생성 완료! 남은 적 소탕 시작.");
    }

    private void SpawnSingleEnemy(GameObject prefab, Vector3 position)
    {
        GameObject enemyObj = Instantiate(prefab, position, Quaternion.identity);

        spawnedEnemyCount++;
        activeEnemyCount++;

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.SetSpawner(this);
            activeEnemies.Add(enemyScript);
        }

        if (enemyObj.TryGetComponent(out EnemyController controller))
        {
            controller.ActivateEnemy();
        }
    }

    private void StopAllAllCoroutinesBeforeNewWave(int totalCount)
    {
        StopAllCoroutines();
        totalEnemiesToSpawn = totalCount;
        spawnedEnemyCount = 0;
        activeEnemyCount = 0;
        isAllSpawned = false;
        activeEnemies.Clear();
    }

    public void OnEnemyDestroyed()
    {
        activeEnemyCount--;
        Debug.Log($"<color=green>[Spawner]</color> 적 처치됨! 필드에 남은 적: {activeEnemyCount} / {totalEnemiesToSpawn}");

        if (isAllSpawned && activeEnemyCount <= 0)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        Debug.Log("<color=yellow>[Spawner] 필드의 모든 적이 소멸되었습니다! 스테이지 클리어 연출 시작.</color>");
        if (GameEndManager.Instance != null)
        {
            GameEndManager.Instance.TriggerGameEnd(GameEndReason.StageCleared);
        }
    }

    private void KillAllEnemiesCheat()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0) return;

        Debug.Log($"<color=red>[Cheat]</color> 치트키 발동! 필드의 모든 적({allEnemies.Length}마리) 처치.");
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null) enemy.Die();
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