using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class Barricade : MonoBehaviour
    {
        public static event Action OnAnyBarricadeDestroyed;

        // 노드 참조 → 바리케이드 (신버전, 좌표 변환 없이 정확)
        public static readonly Dictionary<GraphNode, Barricade> NodeLookup
            = new Dictionary<GraphNode, Barricade>();

        // 격자 셀 → 바리케이드 (구버전 BarricadeBreaker 호환용)
        public static readonly Dictionary<Vector2Int, Barricade> Lookup
            = new Dictionary<Vector2Int, Barricade>();

        public static readonly List<Barricade> All = new List<Barricade>();

        public const uint BarricadeTag = 1;

        public static Vector2Int WorldToCell(Vector3 pos)
            => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));

        [Header("바리케이드 능력치")]
        public float maxHP = 200f;
        private float hp;

        private readonly List<GraphNode> myNodes = new List<GraphNode>();

        void OnEnable()
        {
            hp = maxHP;
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            ClearLookups();
        }

        // 콜라이더 영역 안 모든 노드에 태그 부여 (walkable은 유지!)
        public void RegisterToGraph()
        {
            if (AstarPath.active == null) return;

            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"[Barricade] {name}: Collider 없음");
                return;
            }
            Bounds bb = col.bounds;

            AstarPath.active.AddWorkItem(ctx =>
            {
                myNodes.Clear();
                GridGraph grid = AstarPath.active.data.gridGraph;
                if (grid == null) return;

                grid.GetNodes(node =>
                {
                    Vector3 p = (Vector3)node.position;
                    if (p.x >= bb.min.x && p.x <= bb.max.x &&
                        p.z >= bb.min.z && p.z <= bb.max.z)
                    {
                        node.Tag = BarricadeTag;      // walkable 유지
                        myNodes.Add(node);
                        NodeLookup[node] = this;
                        Lookup[WorldToCell(p)] = this;   // 구버전 호환
                    }
                });

                Debug.Log($"[Barricade] {name} → {myNodes.Count}칸 점유");
            });
        }

        public void TakeDamage(float damage)
        {
            hp -= damage;
            if (hp <= 0f) DestroyBarricade();
        }

        private void DestroyBarricade()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (AstarPath.active != null && myNodes.Count > 0)
            {
                List<GraphNode> nodes = new List<GraphNode>(myNodes);
                AstarPath.active.AddWorkItem(ctx =>
                {
                    foreach (GraphNode n in nodes)
                        if (n != null) n.Tag = 0;
                });
            }

            ClearLookups();
            All.Remove(this);

            OnAnyBarricadeDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void ClearLookups()
        {
            foreach (GraphNode n in myNodes)
            {
                if (n == null) continue;

                if (NodeLookup.TryGetValue(n, out var b1) && b1 == this)
                    NodeLookup.Remove(n);

                Vector2Int cell = WorldToCell((Vector3)n.position);
                if (Lookup.TryGetValue(cell, out var b2) && b2 == this)
                    Lookup.Remove(cell);
            }
        }

        void OnDrawGizmos()
        {
            if (myNodes == null || myNodes.Count == 0) return;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            foreach (var n in myNodes)
            {
                if (n == null) continue;
                Gizmos.DrawCube((Vector3)n.position, new Vector3(0.9f, 0.2f, 0.9f));
            }
        }
    }
}