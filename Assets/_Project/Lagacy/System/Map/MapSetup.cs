using System.Collections;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class MapSetup : MonoBehaviour
    {
        IEnumerator Start()
        {
            Debug.Log("[MapSetup] Start 진입");           // ✨ 1

            yield return null;

            if (AstarPath.active == null)
            {
                Debug.LogError("[MapSetup] AstarPath.active가 null — Pathfinder 오브젝트 확인");
                yield break;
            }
            Debug.Log("[MapSetup] Scan 시작");             // ✨ 2
            AstarPath.active.Scan();

            var bars = FindObjectsByType<Barricade>(FindObjectsSortMode.None);
            foreach (var b in bars) b.RegisterToGraph();
            Debug.Log($"[MapSetup] 바리케이드 {bars.Length}개 등록 요청");  // ✨ 3

            yield return null;

            if (EnemyPathfinding.Instance == null)
                Debug.LogError("[MapSetup] EnemyPathfinding.Instance가 null");
            else
                EnemyPathfinding.Instance.Initialize();

            Debug.Log("[MapSetup] 준비 완료");             // ✨ 4
        }
    }
}