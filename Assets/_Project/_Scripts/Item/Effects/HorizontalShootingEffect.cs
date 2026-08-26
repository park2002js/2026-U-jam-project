using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Item
{
    [CreateAssetMenu(fileName = "Item_001_HorizontalEffect", menuName = "Game/Items/Effects/Horizontal Shooting")]
    public class HorizontalShootingEffect : ItemEffect
    {
        [Header("가로 원통: 월드 X -16 ~ +16")]
        [SerializeField, Min(0.01f)] private float radius = 0.5f;
        [SerializeField, Min(0.01f)] private float visualLifetime = 0.2f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        public override bool IsShootingHitEffect => true;

        public override void Apply(ItemUseContext context)
        {
            if (!context.IsShootingHit || context.DamageInfo.Damage <= 0f) return;
            if (!float.IsFinite(radius) || radius <= 0f || !float.IsFinite(visualLifetime) || visualLifetime <= 0f) return;
            if (VisualPrefab == null || !VisualPrefab.TryGetComponent(out MeshCollider prefabCollider) || prefabCollider.sharedMesh == null || !prefabCollider.convex)
            {
                Debug.LogError("[HorizontalShootingEffect] Visual Prefab 루트에 Cylinder Mesh를 사용하는 Convex MeshCollider가 필요합니다.", this);
                return;
            }

            // Unity 기본 Cylinder(로컬 Y 높이 2, 반지름 0.5)를 월드 X축 길이 32로 배치한다.
            Vector3 center = new Vector3(0f, context.HitPoint.y, context.HitPoint.z);
            Quaternion rotation = Quaternion.Euler(0f, 0f, 90f);
            MeshCollider cylinder = Instantiate(prefabCollider, center, rotation);
            cylinder.transform.localScale = new Vector3(radius * 2f, 16f, radius * 2f);
            foreach (Collider collider in cylinder.GetComponentsInChildren<Collider>()) collider.enabled = false;
            Destroy(cylinder.gameObject, visualLifetime);

            // 박스는 후보 수집용이며 실제 판정은 양 끝이 평평한 원통의 Convex Mesh로 한다.
            Physics.SyncTransforms(); // Grid 이동으로 바뀐 현재 위치를 도착 순간의 Overlap에 반영한다.
            Collider[] candidates = Physics.OverlapBox(center, new Vector3(16f, radius, radius), Quaternion.identity, enemyLayers, QueryTriggerInteraction.Collide);
            var enemies = new HashSet<EnemyBase>();
            foreach (Collider candidate in candidates)
            {
                EnemyBase enemy = candidate.GetComponentInParent<EnemyBase>();
                if (enemy == null || enemies.Contains(enemy)) continue;
                if (Physics.ComputePenetration(cylinder, center, rotation, candidate, candidate.transform.position, candidate.transform.rotation, out _, out _)) enemies.Add(enemy);
            }

            // 원래 총알에 맞은 적도 제외하지 않는다. 도착 순간 겹친 적마다 같은 DamageInfo를 추가 전달한다.
            foreach (EnemyBase enemy in enemies)
                if (enemy != null) enemy.TakeDamage(context.DamageInfo);
        }
    }
}
