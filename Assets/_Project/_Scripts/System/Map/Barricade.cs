using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace EnemySystem
{
    public class Barricade : MonoBehaviour
    {
        // 누군가 바리케이드를 부수면 모든 적이 이 이벤트로 재탐색
        public static event Action OnAnyBarricadeDestroyed;

        // 격자 셀 → 바리케이드 조회용
        public static readonly Dictionary<Vector2Int, Barricade> Lookup
            = new Dictionary<Vector2Int, Barricade>();

        // 이 태그를 가진 노드 = 바리케이드 (A* Grid Graph의 Tag, 0~31)
        public const uint BarricadeTag = 1;

        [Header("바리케이드 능력치")]
        public float maxHP = 200f;
        private float hp;

        private GraphNode node;
        private Vector2Int cell;

        public static Vector2Int WorldToCell(Vector3 pos)
            => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z));

        void OnEnable() { hp = maxHP; }

        // 웨이브 시작(Scan 완료) 직후 호출 → 자기 노드를 바리케이드 태그로 등록
        public void RegisterToGraph()
        {
            if (AstarPath.active == null) return;

            AstarPath.active.AddWorkItem(ctx =>
            {
                node = AstarPath.active.GetNearest(transform.position, NNConstraint.None).node;
                if (node == null) return;

                node.Tag = BarricadeTag;          // 노드는 walkable 유지, 태그만 부여
                cell = WorldToCell((Vector3)node.position);
                Lookup[cell] = this;
            });
        }

        // 적이 때리면 호출됨 (Enemy/BarricadeBreaker에서 호출)
        public void TakeDamage(float damage)
        {
            hp -= damage;
            if (hp <= 0f) DestroyBarricade();
        }

        private void DestroyBarricade()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 노드 태그 제거 + walkable 확정 (work item으로 안전하게)
            if (AstarPath.active != null && node != null)
            {
                GraphNode n = node;
                AstarPath.active.AddWorkItem(ctx =>
                {
                    n.Tag = 0;
                    n.Walkable = true;
                });
            }

            Lookup.Remove(cell);
            // 이벤트로 인한 경로 요청은 위 work item 뒤에 큐잉 → 갱신된 그래프를 반영
            OnAnyBarricadeDestroyed?.Invoke();
            Destroy(gameObject);
        }

        void OnDisable()
        {
            if (Lookup.TryGetValue(cell, out var b) && b == this)
                Lookup.Remove(cell);
        }
    }
}