using UnityEngine;

public class WaveEntry : MonoBehaviour
{
    public GameObject enemyPrefab; // 생성할 적 프리팹
    public int enemyCount = 5; // 생성할 적의 수
    public float spawnInterval = 2.0f; // 적 생성 간격(초)

    public EnemySpawner spawner; // 적 생성기 참조

    public void StartWave() //웨이브 시작 메서드
    {
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetTotalEnemies(enemyCount); //battle manager에 총 적 수 설정
        }

        if (spawner != null)
        {
            spawner.StartSpawning(enemyPrefab, enemyCount, spawnInterval);  //적 스폰 시작
        }
    }

    private void Start()
    {
        StartWave();
    }
}