using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SpawnInfo
{
    [Tooltip("소환할 적 프리팹")]
    public GameObject enemyPrefab;
    [Tooltip("소환 간격")]
    public float spawnInterval;
    [Tooltip("스폰 딜레이")]
    public float startDelay;
    [Tooltip("스포너 위치 (0, 0, 0) 기준 상대 소환 좌표")]
    public List<Vector3> spawnOffset;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "WaveSystem/WaveData")]
public class WaveData : ScriptableObject
{
    public List<SpawnInfo> spawnList;

    public int GetTotalEnemyCount()
    {
        int total = 0;
        if (spawnList == null) return 0;
        foreach(var info in spawnList)
        {
            if(info.spawnOffset != null)
            {
                total += info.spawnOffset.Count;
            }
        }
        return total;
    }
}