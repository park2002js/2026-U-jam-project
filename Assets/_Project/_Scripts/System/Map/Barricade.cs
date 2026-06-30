using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class Barricade : MonoBehaviour
    {
        public static event Action OnAnyBarricadeDestroyed;

        public static readonly Dictionary<Vector2Int, Barricade> Lookup
            = new Dictionary<Vector2Int, Barricade>();
        public static readonly List<Barricade> All = new List<Barricade>();

        public const uint BarricadeTag = 1;

        [Header("바리케이드 능력치")]
        public float maxHP = 200f;
        private float hp;

        private readonly List<GraphNode> myNodes = new List<GraphNode>();
        private readonly List<Vector2Int> myCells = new List<Vector2Int>();

        public static Vector2Int WorldToCell(Vector3 pos)
            => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));

        void OnEnable()
        {
            hp = maxHP;
            if (!All.Contains(this)) All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            foreach (Vector2Int c in myCells)
                if (Lookup.TryGetValue(c, out var b) && b == this)
                    Lookup.Remove(c);
        }

        // 등록: 콜라이더 영역 안 모든 노드에 '태그'만 부여 (walkable 유지!)
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
                myCells.Clear();

                GridGraph grid = AstarPath.active.data.gridGraph;
                if (grid == null) return;

                grid.GetNodes(node =>
                {
                    Vector3 p = (Vector3)node.position;
                    if (p.x >= bb.min.x && p.x <= bb.max.x &&
                        p.z >= bb.min.z && p.z <= bb.max.z)
                    {
                        node.Tag = BarricadeTag;   // 태그만! walkable은 true 유지
                        myNodes.Add(node);
                        Vector2Int cell = WorldToCell(p);
                        myCells.Add(cell);
                        Lookup[cell] = this;
                    }
                });

                Debug.Log($"[Barricade] {name} → {myNodes.Count}개 노드 점유(태그)");
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
                        if (n != null) n.Tag = 0;   // 태그만 제거
                });
            }

            foreach (Vector2Int c in myCells)
                if (Lookup.TryGetValue(c, out var b) && b == this)
                    Lookup.Remove(c);
            All.Remove(this);

            OnAnyBarricadeDestroyed?.Invoke();
            Destroy(gameObject);
        }

        void OnDrawGizmos()
        {
            if (myNodes == null || myNodes.Count == 0) return;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            foreach (var n in myNodes)
            {
                if (n == null) continue;
                Vector3 p = (Vector3)n.position;
                Gizmos.DrawCube(p, new Vector3(0.9f, 0.2f, 0.9f));
            }
        }
    }
}