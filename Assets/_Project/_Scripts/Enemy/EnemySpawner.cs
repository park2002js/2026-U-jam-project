using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using EnemySystem; // [추가] Enemy 스크립트가 속한 네임스페이스 연결

/*
     EnemySpawer에 생성로직을 다음과 같이 변경 : 적의 종류와 그 갯수를 미리 할당하면, 그 숫자만큼 비동기로 동시에 생성
    -> 이후 전투 페이즈가 되면 생성한 적들을 모두 활성화 시킴. 적들의 위치는 Spawner가 할당받은 width에서 랜덤한 위치에 생성, (한 줄을 유지함) 
*/
[System.Serializable]
public class SpawnInfo
{
    public GameObject enemyPrefab;
    public int spawnCount;
}

public class EnemySpawner : MonoBehaviour
{
    // [추가] 외부(Enemy.cs 등)에서 스포너에 접근할 수 있도록 싱글톤 뼈대 배치
    public static EnemySpawner Instance { get; private set; }

    [Header("Spawn Area Settings (3D Region)")]
    public float minXOffset;
    public float maxXOffset;
    public float spawnHeight;

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


    [Header("Wave Spawn Settings")]
    [Tooltip("여기에 이번 웨이브에 소환할 적 프리팹과 마릿수를 추가하세요.")]
    public List<SpawnInfo> waveSpawnList = new List<SpawnInfo>();

    // [추가] 생성 후 대기 중인 적들을 보관할 리스트
    private List<GameObject> standbyEnemies = new List<GameObject>();

    private void OnEnable()
    {
        StopAllCoroutines();

        // [추가] 새로운 소환 명령이 내려올 때 카운팅 변수들 초기화
        totalEnemiesToSpawn = count;
        spawnedEnemyCount = 0;
        activeEnemyCount = 0;
        isAllSpawned = false;

        StartCoroutine(SpawnRoutine(prefab, count, interval));
    }

    // // 1. 비동기 동시 생성 (대기 상태로 스폰)
    // public void PreSpawnEnemies(GameObject prefab, int count)
    // {
    //     StopAllCoroutines();
    //     StartCoroutine(PreSpawnRoutine(prefab, count));
    // }

    // private IEnumerator PreSpawnRoutine(GameObject prefab, int count)
    // {
    //     Debug.Log($"[Spawner] {count}마리 비동기 사전 생성 시작...");

    //     for (int i = 0; i < count; i++)
    //     {
    //         // 한 줄(Line) 기반의 랜덤 위치 가져오기
    //         Vector3 spawnPosition = GetRandomPositionInLine();
    //         GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

    //         // ✨ [수정] 생성 직후 비활성화하여 맵에 보이지 않게 대기 리스트에 넣음
    //         enemy.SetActive(false);
    //         standbyEnemies.Add(enemy);

    //         // 한 프레임에 수십 마리를 동시에 생성하면 렉(프레임 드랍)이 발생하므로, 
    //         // 5마리 생성할 때마다 프레임을 한 번씩 쉬어주어 비동기적으로 부드럽게 생성합니다.
    //         if (i % 5 == 0)
    //         {
    //             yield return null;
    //         }
    //     }

    //     Debug.Log("[Spawner] 사전 생성 완료. 전투 페이즈 대기 중...");
    // }
    private IEnumerator PreSpawnAllRoutine()
    {
        Debug.Log("[Spawner] 웨이브 리스트 기반 비동기 사전 생성 시작...");
        int totalSpawned = 0;

        // 리스트에 등록된 모든 몬스터 종류를 순회
        foreach (SpawnInfo info in waveSpawnList)
        {
            if (info.enemyPrefab == null || info.spawnCount <= 0) continue;

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
                Vector3 spawnPosition = GetRandomPositionInLine();
                GameObject enemy = Instantiate(info.enemyPrefab, spawnPosition, Quaternion.identity);

                enemy.SetActive(false); // 맵에 보이지 않게 숨김
                standbyEnemies.Add(enemy);
                totalSpawned++;

                // 섞어서 소환하더라도 누적 5마리마다 프레임을 쉬어주어 렉 방지
                if (totalSpawned % 5 == 0)
                {
                    yield return null;
                }
            }
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


    // 2. 전투 페이즈 시작 시 일괄 활성화
    public void ActivateAllEnemies()
    {
        Debug.Log($"[Spawner] 대기 중인 적 {standbyEnemies.Count}마리 일괄 활성화!");

        foreach (var enemy in standbyEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true); // 맵에 표시
                
                // 컨트롤러 활성화 명령
                if (enemy.TryGetComponent(out EnemyController controller))
                {
                    controller.ActivateEnemy();
                }
            }
        }
        
        // 활성화가 끝났으므로 리스트 비우기
        standbyEnemies.Clear();
    }


    // "한 줄" 유지를 위해 X축은 중앙값으로 고정하고, Z축으로만 랜덤하게 배치
    // ✨ [수정] 스포너의 회전(Rotation) 방향을 반영하여 한 줄의 위치를 결정합니다.
    private Vector3 GetRandomPositionInLine()
    {
        // 1. 로컬 기준의 위치 계산 (X는 고정, Z는 랜덤)
        float fixedX = (minXOffset + maxXOffset) / 2f; 
        float randomZ = Random.Range(-spawnHeight / 2f, spawnHeight / 2f);

        // 2. 높이를 제외한 순수 평면 오프셋
        Vector3 localOffset = new Vector3(fixedX, 0f, randomZ);

        // 3. 스포너의 현재 회전값을 곱하여 로컬 방향을 월드 방향으로 변환! (이게 핵심입니다)
        Vector3 rotatedOffset = transform.rotation * localOffset;

        // 4. 스포너 위치 + 회전된 오프셋 + 하늘 위로 띄우기(Raycast용)
        Vector3 rayOrigin = transform.position + rotatedOffset + (Vector3.up * 10f);
        Vector3 finalPos = rayOrigin;

        // // 아래로 Ray를 쏴서 땅바닥 높이 찾기
        // if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        // {
        //     finalPos = hit.point;
        // }
        // else
        // {
        //     finalPos.y = transform.position.y;
        // }
        finalPos.y = transform.position.y;

        return finalPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        // 기즈모의 렌더링 매트릭스를 이 오브젝트의 로컬 좌표계로 변경하여 회전을 적용시킴
        Gizmos.matrix = transform.localToWorldMatrix;

        // 로컬 좌표 기준이므로 현재 오브젝트의 중심점은 (0,0,0) 취급됨
        float fixedX = (minXOffset + maxXOffset) / 2f;
        Vector3 localCenter = new Vector3(fixedX, 0f, 0f); 
        Vector3 size = new Vector3(0.5f, 1f, spawnHeight);
        
        Gizmos.DrawWireCube(localCenter, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        Gizmos.DrawCube(localCenter, size);
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