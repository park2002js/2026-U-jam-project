using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Area Settings (3D Region)")]
    public float minXOffset = -2f;
    public float maxXOffset = -4f;
    public float spawnHeight = 10f;

    [Header("Physics Settings")]
    public LayerMask groundLayer;

    [Header("Gizmo Settings")]
    public Color gizmoColor = Color.cyan;

    // [수정] Start()와 LaunchTest()를 제거했습니다. 
    // 이제 외부(WaveEntry)에서 명령을 내릴 때만 적이 생성됩니다.

    public void StartSpawning(GameObject prefab, int count, float interval)
    {
        // [중요] 이전 소환 루틴이 돌고 있다면 멈춰서 중복 생성을 방지합니다.
        StopAllCoroutines();
        StartCoroutine(SpawnRoutine(prefab, count, interval));
    }

    private IEnumerator SpawnRoutine(GameObject prefab, int count, float interval)
    {
        Debug.Log($"[Spawner] 소환 시작: {count}마리");

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionIn3DRegion();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            // EnemyController가 있다면 활성화 (없어도 에러 안 남)
            if (enemy.TryGetComponent(out EnemyController controller))
            {
                controller.ActivateEnemy();
            }

            yield return new WaitForSeconds(interval);
        }

        Debug.Log("[Spawner] 모든 적 생성 완료");
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