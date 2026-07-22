using UnityEngine;
using System.Collections;
using Pathfinding;

namespace EnemySystem
{
    public class BarricadeRegister : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;

            AstarPath.active.Scan();

            Barricade[] barricades = FindObjectsByType<Barricade>(FindObjectsSortMode.None);
            foreach (Barricade b in barricades)
                b.RegisterToGraph();

            Debug.Log($"[Registrar] 등록 완료 — 바리케이드 {barricades.Length}개");

            yield return null; // work item 처리 대기

            BarricadeBreaker[] breakers = FindObjectsByType<BarricadeBreaker>(FindObjectsSortMode.None);
            foreach (BarricadeBreaker br in breakers)
                br.Recalculate();

            Debug.Log($"[Registrar] 적 {breakers.Length}마리 재탐색");
        }
    }
}