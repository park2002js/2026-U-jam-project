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

    [Header("Test Settings")]
    public GameObject testEnemyPrefab; // 클래스 내부로 이동됨

    public Color gizmoColor = Color.cyan;

    // 1. 게임 시작 시 호출되는 부분 (클래스 내부로 이동됨)
    void Start()
    {
        if (testEnemyPrefab != null)
        {
            // 2초 뒤에 테스트 소환 시작
            Invoke("LaunchTest", 2f);
        }
    }

    void LaunchTest()
    {
        StartSpawning(testEnemyPrefab, 5, 1.0f);
    }

    public void StartSpawning(GameObject prefab, int count, float interval)
    {
        StartCoroutine(SpawnRoutine(prefab, count, interval));
    }

    private IEnumerator SpawnRoutine(GameObject prefab, int count, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionIn3DRegion();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (enemy.TryGetComponent(out EnemyController controller))
            {
                controller.ActivateEnemy();
            }

            yield return new WaitForSeconds(interval);
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