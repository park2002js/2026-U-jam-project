using UnityEngine;
using EnemySystem;

public class WaveEntry : MonoBehaviour
{
    public GameObject enemyPrefab;      // 생성할 적 프리팹
    public int enemyCount = 5;         // 생성할 적의 수
    public float spawnInterval = 0.5f; // 적 생성 간격 (0.1초는 너무 빠를 수 있어 0.5초 추천)

    public EnemySpawner spawner;       // 적 생성기 참조

    public void StartWave()
    {
        Debug.Log($"[WaveEntry] 명령 전달 중: 마릿수 {enemyCount}");

        // 1. 배틀 매니저가 있다면 총 적 마릿수 세팅 (기존 로직 유지)
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetTotalEnemies(enemyCount);
        }

        // 2. [핵심 추가] 우리가 오늘 완성한 EnemySpawner에게 진짜 소환 명령을 내립니다!
        if (spawner != null)
        {
            spawner.StartSpawning(enemyPrefab, enemyCount, spawnInterval);
        }
        else
        {
            // 만약 인스펙터에서 깜빡하고 지정을 안 했다면 싱글톤으로 찾아서라도 실행하는 안전장치
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.StartSpawning(enemyPrefab, enemyCount, spawnInterval);
            }
            else
            {
                Debug.LogError("[WaveEntry] 씬에 스포너가 없거나 연결되지 않았습니다!");
            }
        }
    }
}