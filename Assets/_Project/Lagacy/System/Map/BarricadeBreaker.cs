using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class BarricadeBreaker : MonoBehaviour
    {
        private Enemy enemy;
        private Transform baseTarget;

        [Tooltip("경로 끝점이 베이스에 이 거리 안이면 도달로 판단")]
        public float reachThreshold = 2f;
        public float recalcCooldown = 0f;
        private float lastRecalc = -999f;
        private float attackTimer = 0f;

        private Barricade targetBarricade;
        private Vector3 attackPoint;   // 벽 칸 (공격 거리 판정용)

        // 1차 탐색용: 바리케이드 태그 노드를 '벽'으로 취급
        private class BlockBarricade : ITraversalProvider
        {
            public bool CanTraverse(Path path, GraphNode node)
                => node.Walkable && node.Tag != Barricade.BarricadeTag;
            public uint GetTraversalCost(Path path, GraphNode node) => 0;
        }
        private class AllowBarricade : ITraversalProvider
        {
            public bool CanTraverse(Path path, GraphNode node) 
                => node.Walkable || node.Tag == Barricade.BarricadeTag; // 막혀있어도 태그가 있으면 통과 허용!
            public uint GetTraversalCost(Path path, GraphNode node) => 0;
        }
        private static readonly AllowBarricade allowProvider = new AllowBarricade();

        private static readonly BlockBarricade blockProvider = new BlockBarricade();

        void Awake() { enemy = GetComponent<Enemy>(); }

        void OnEnable()  { Barricade.OnAnyBarricadeDestroyed += Recalculate; }
        void OnDisable() { Barricade.OnAnyBarricadeDestroyed -= Recalculate; }

        void Start()
        {
            GameObject b = GameObject.FindGameObjectWithTag("Base");
            if (b != null) baseTarget = b.transform;
            // 경로 요청은 BarricadeRegister가 등록 후 일괄로 시킴
        }

        public void Recalculate()
        {
            if (baseTarget == null || AstarPath.active == null) return;
            if (Time.time - lastRecalc < recalcCooldown) return;
            lastRecalc = Time.time;

            // 1차: 바리케이드를 벽으로 막고 우회로 탐색
            ABPath blocked = ABPath.Construct(transform.position, baseTarget.position, OnBlockedComplete);
            blocked.traversalProvider = blockProvider;
            AstarPath.StartPath(blocked);
        }

        private void OnBlockedComplete(Path p)
        {
            if (!p.error && ReachedBase(p))
            {
                // 우회로 있음 → 베이스로 직행
                enemy.ClearForcedPoint();
                targetBarricade = null;
                return;
            }

            // 우회로 없음 → 2차: 바리케이드 통과 허용(provider 없음) 탐색
            ABPath open = ABPath.Construct(transform.position, baseTarget.position, OnOpenComplete);
            open.traversalProvider = allowProvider;
            AstarPath.StartPath(open);
        }

        private void OnOpenComplete(Path p)
        {
            if (p.error || p.path == null)
            {
                enemy.ClearForcedPoint();
                targetBarricade = null;
                return;
            }

            // 진행방향 첫 바리케이드 = 부술 대상, 그 직전 walkable 칸 = 서는 위치
            GraphNode prevNode = null;
            foreach (GraphNode n in p.path)
            {
                if (n.Tag == Barricade.BarricadeTag)
                {
                    Vector2Int cell = Barricade.WorldToCell((Vector3)n.position);
                    if (Barricade.Lookup.TryGetValue(cell, out Barricade b) && b != null)
                    {
                        targetBarricade = b;
                        attackPoint = (Vector3)n.position;

                        Vector3 standPoint = (prevNode != null)
                            ? (Vector3)prevNode.position
                            : transform.position;

                        // 이동=서는 곳, 공격 거리 기준=벽 칸
                        enemy.SetForcedPoint(standPoint, attackPoint);
                        return;
                    }
                }
                prevNode = n;
            }

            enemy.ClearForcedPoint();
            targetBarricade = null;
        }

        void Update()
        {
            if (targetBarricade == null) return;

            if (targetBarricade.Equals(null))
            {
                targetBarricade = null;
                enemy.ClearForcedPoint();
                Recalculate();
                return;
            }

            // Y축 무시 거리 계산
            Vector3 myPos2D = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 attackPos2D = new Vector3(attackPoint.x, 0, attackPoint.z);

            float dist = Vector3.Distance(transform.position, attackPoint);
            if (dist <= enemy.attackRange + 0.5f)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    targetBarricade.TakeDamage(enemy.AD);
                    Debug.Log($"[Breaker] {name} 공격 → {targetBarricade.name} 데미지={enemy.AD}");
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