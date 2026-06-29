using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class BarricadeBreaker : MonoBehaviour
    {
        private Enemy enemy;
        private Transform baseTarget;

        [Tooltip("경로 끝점이 베이스에 이 거리 안이면 '도달 가능'으로 판단")]
        public float reachThreshold = 2f;
        [Tooltip("재탐색 최소 간격(이벤트 폭주 방지)")]
        public float recalcCooldown = 0.2f;
        private float lastRecalc = -999f;
        private float attackTimer = 0f;

        // 바리케이드를 '벽'으로 막는 탐색용 provider (1차 탐색 전용)
        private class BlockBarricade : ITraversalProvider
        {
            public bool CanTraverse(Path path, GraphNode node)
                => node.Walkable && node.Tag != Barricade.BarricadeTag;
            public uint GetTraversalCost(Path path, GraphNode node) => 0;
        }
        private static readonly BlockBarricade blockProvider = new BlockBarricade();

        void Awake() { enemy = GetComponent<Enemy>(); }

        void OnEnable()  { Barricade.OnAnyBarricadeDestroyed += Recalculate; }
        void OnDisable() { Barricade.OnAnyBarricadeDestroyed -= Recalculate; }

        void Start()
        {
            GameObject b = GameObject.FindGameObjectWithTag("Base");
            if (b != null) baseTarget = b.transform;
            Recalculate();
        }

        public void Recalculate()
        {
            if (baseTarget == null || AstarPath.active == null) return;
            if (Time.time - lastRecalc < recalcCooldown) return;
            lastRecalc = Time.time;

            // 1차: 바리케이드를 벽으로 간주
            ABPath blocked = ABPath.Construct(transform.position, baseTarget.position, OnBlockedComplete);
            blocked.traversalProvider = blockProvider;
            AstarPath.StartPath(blocked);   // ✨ active. 제거
        }

        private void OnBlockedComplete(Path p)
        {
            if (!p.error && ReachedBase(p))
            {
                enemy.forcedTarget = null; // 우회로 있음 → 베이스로 직행
                return;
            }
            // 2차: 바리케이드 통과 허용(기본 동작) → 최단 경로상 첫 바리케이드 탐색
            ABPath open = ABPath.Construct(transform.position, baseTarget.position, OnOpenComplete);
            AstarPath.StartPath(open);   // ✨ active. 제거
        }

        private void OnOpenComplete(Path p)
        {
            if (p.error || p.path == null) { enemy.forcedTarget = null; return; }

            Barricade first = null;
            foreach (GraphNode n in p.path)
            {
                if (n.Tag != Barricade.BarricadeTag) continue;
                Vector2Int cell = Barricade.WorldToCell((Vector3)n.position);
                if (Barricade.Lookup.TryGetValue(cell, out Barricade b) && b != null)
                {
                    first = b; break;
                }
            }
            // 첫 바리케이드를 강제 목표로 (없으면 베이스)
            enemy.forcedTarget = (first != null) ? first.transform : null;
        }

        void Update()
        {
            // 바리케이드를 목표로 잡았고, 사거리 안이면 직접 공격
            Transform t = enemy.forcedTarget;
            if (t == null) return;

            Barricade b = t.GetComponent<Barricade>();
            if (b == null) return;

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist <= enemy.attackRange + 0.5f)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    b.TakeDamage(enemy.AD);
                    attackTimer = 1f / Mathf.Max(0.01f, enemy.AS);
                }
            }
        }

        private bool ReachedBase(Path p)
        {
            if (p.vectorPath == null || p.vectorPath.Count == 0) return false;
            Vector3 end = p.vectorPath[p.vectorPath.Count - 1];
            return Vector3.Distance(end, baseTarget.position) <= reachThreshold;
        }
    }
}