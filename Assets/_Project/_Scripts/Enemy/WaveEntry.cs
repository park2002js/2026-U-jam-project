using UnityEngine;
using EnemySystem;
public class WaveEntry : MonoBehaviour
{
    public GameObject enemyPrefab; // 생성할 적 프리팹
    public int enemyCount = 5;     // 생성할 적의 수
    public float spawnInterval = 0.1f; // 적 생성 간격(초)

    public EnemySpawner spawner;   // 적 생성기 참조

    public void StartWave()
    {
        // 콘솔창에 숫자가 정확히 출력되는지 확인하는 로그
        Debug.Log($"[WaveEntry] 명령 전달 중: 마릿수 {enemyCount}");

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetTotalEnemies(enemyCount);
        }

        if (spawner != null)
        {
            spawner.StartSpawning(enemyPrefab, enemyCount, spawnInterval);
        }
    }

    // 테스트를 위해 게임 시작 시 자동으로 StartWave를 실행하게 함
    private void Start()
    {
        StartWave();
    }
}