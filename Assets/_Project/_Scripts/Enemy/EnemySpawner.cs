using UnityEngine;
using System.Collections;
using EnemySystem; 
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Area Settings (3D Region)")]
    public float minXOffset;
    public float maxXOffset;
    public float spawnHeight;

    [Header("Physics Settings")]
    public LayerMask groundLayer;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;

    private int totalEnemiesToSpawn = 0; 
    private int spawnedEnemyCount = 0;   
    private int activeEnemyCount = 0;    
    private bool isAllSpawned = false;   


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Wave Spawn Settings")]
    [Tooltip("여기에 이번 웨이브에 소환할 적 프리팹과 마릿수를 추가하세요.")]
    public List<SpawnInfo> waveSpawnList = new List<SpawnInfo>();

    private List<GameObject> standbyEnemies = new List<GameObject>();

    [System.Serializable]
    public struct SpawnInfo
    {
        [Header("소환할 적 프리팹")]
        public GameObject enemyPrefab; 
        
        [Header("소환할 마리 수")]
        public int spawnCount;
        
        [Header("소환 간격 (초)")]
        public float spawnInterval;
    }   

    private void OnEnable()
    {
        StopAllCoroutines();

        // 1. 에러 수정: 리스트를 순회하며 이번 웨이브에 소환할 총 마리 수를 계산합니다.
        totalEnemiesToSpawn = 0;
        foreach (SpawnInfo info in waveSpawnList)
        {
            totalEnemiesToSpawn += info.spawnCount;
        }

        spawnedEnemyCount = 0;
        activeEnemyCount = 0;
        isAllSpawned = false;

        // 2. 에러 수정: 옛날 함수 대신 비동기 소환 루틴을 실행합니다.
        StartCoroutine(PreSpawnAllRoutine());
    }

    private IEnumerator PreSpawnAllRoutine()
    {
        Debug.Log("[Spawner] 웨이브 리스트 기반 비동기 사전 생성 시작...");
        int totalSpawned = 0;

        foreach (SpawnInfo info in waveSpawnList)
        {
            if (info.enemyPrefab == null || info.spawnCount <= 0) continue;

            // 3. 에러 수정: 누락되어 있던 반복문을 추가하여 spawnCount만큼 소환하게 만듭니다.
            for (int i = 0; i < info.spawnCount; i++)
            {
                spawnedEnemyCount++;
                activeEnemyCount++;

                Vector3 spawnPosition = GetRandomPositionInLine();
                GameObject spawnedEnemy = Instantiate(info.enemyPrefab, spawnPosition, Quaternion.identity);

                // ✨ Enemy.cs를 건드리지 않기 위해 SetSpawner 코드는 완전히 삭제했습니다!

                spawnedEnemy.SetActive(false); // 맵에 보이지 않게 숨김
                standbyEnemies.Add(spawnedEnemy);
                totalSpawned++;

                if (totalSpawned % 5 == 0)
                {
                    yield return null;
                }
            }
        }

        isAllSpawned = true;
        Debug.Log("[Spawner] 모든 적 생성 완료. 남은 적들을 소탕해야 합니다.");
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
        Debug.Log("<color=yellow>[Spawner] 필드의 모든 적이 소멸되었습니다! 스테이지 클리어!</color>");
        GameEndManager.Instance.TriggerGameEnd(GameEndReason.StageCleared);
    }

    public void ActivateAllEnemies()
    {
        Debug.Log($"[Spawner] 대기 중인 적 {standbyEnemies.Count}마리 일괄 활성화!");

        foreach (var enemy in standbyEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true); 
                
                if (enemy.TryGetComponent(out EnemyController controller))
                {
                    controller.ActivateEnemy();
                }
            }
        }
        standbyEnemies.Clear();
    }

    private Vector3 GetRandomPositionInLine()
    {
        float fixedX = (minXOffset + maxXOffset) / 2f; 
        float randomZ = Random.Range(-spawnHeight / 2f, spawnHeight / 2f);

        Vector3 localOffset = new Vector3(fixedX, 0f, randomZ);
        Vector3 rotatedOffset = transform.rotation * localOffset;

        Vector3 rayOrigin = transform.position + rotatedOffset + (Vector3.up * 10f);
        Vector3 finalPos = rayOrigin;
        
        finalPos.y = transform.position.y;
        return finalPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        float fixedX = (minXOffset + maxXOffset) / 2f;
        Vector3 localCenter = new Vector3(fixedX, 0f, 0f); 
        Vector3 size = new Vector3(0.5f, 1f, spawnHeight);
        
        Gizmos.DrawWireCube(localCenter, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(localCenter, size);
    }

    private void Update()
    {
        if(Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillAllEnemiesCheat();
        }
    }

    private void KillAllEnemiesCheat()
    {
        EnemySystem.Enemy[] allEnemies = FindObjectsByType<EnemySystem.Enemy>(FindObjectsSortMode.None);

        if (allEnemies.Length == 0)
        {
            Debug.Log("<color=yellow>[Cheat]</color> 필드에 죽일 적이 없습니다.");
            return;
        }

        Debug.Log($"<color=red>[Cheat]</color> 치트키 발동! 필드의 모든 적({allEnemies.Length}마리)을 처치합니다.");

        foreach (EnemySystem.Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                enemy.Die();
            }
        }
    }
}