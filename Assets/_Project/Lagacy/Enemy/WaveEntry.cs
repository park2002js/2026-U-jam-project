using UnityEngine;
using EnemySystem;

public class WaveEntry : MonoBehaviour
{
    [Header("웨이브 배치 데이터 (ScriptableObject)")]
    [Tooltip("여기에 생성한 WaveData 에셋을 넣어주세요.")]
    public WaveData waveData;          // ◀ [수정] 단일 프리팹 대신 복합 배치 명세서를 받습니다.

    [Header("적 생성기 참조")]
    public EnemySpawner spawner;       // 적 생성기 참조

    public GameObject enemyPrefab
    {
        get
        {
            if(waveData != null && waveData.spawnList != null && waveData.spawnList.Count > 0)
            {
                return waveData.spawnList[0].enemyPrefab; // 첫 번째 스폰 그룹의 프리팹을 기본으로 반환 (기존 로직 호환용)
            }
            return null;
        }
    }

    public void StartWave()
    {

        // 🚨 안전장치: 명세서 데이터가 비어있다면 실행하지 않음
        if (waveData == null)
        {
            Debug.LogError($"[{name}] WaveData가 지정되지 않았습니다! 인스펙터를 확인하세요.");
            return;
        }

        // 💡 명세서에 적힌 모든 스폰 그룹의 적 마릿수 총합을 계산합니다.
        int totalEnemies = waveData.GetTotalEnemyCount();

        Debug.Log($"[WaveEntry] 다중 배치 명령 전달 중: 총 마릿수 {totalEnemies}");

        // 1. 배틀 매니저가 있다면 총 적 마릿수 세팅 (기존 로직 유지 + 정확한 총합 수치 연동)
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.SetTotalEnemies(totalEnemies);
        }

        // 2. [핵심 수정] EnemySpawner에게 명세서(WaveData)를 통째로 넘겨 다중 소환을 처리하게 합니다.
        if (spawner != null)
        {
            spawner.StartWaveFromData(waveData);
        }
        else
        {
            // 만약 인스펙터에서 깜빡하고 지정을 안 했다면 싱글톤으로 찾아서라도 실행하는 안전장치
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.StartWaveFromData(waveData);
            }
            else
            {
                Debug.LogError("[WaveEntry] 씬에 스포너가 없거나 연결되지 않았습니다!");
            }
        }
    }
}