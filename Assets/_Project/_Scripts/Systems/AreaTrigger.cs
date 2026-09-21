using System;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    // OnTrigger 메시지를 일반 C# 효과에 전달하는 물리 어댑터.
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public sealed class AreaTrigger : MonoBehaviour
    {
        /// <summary>현재 영역에 들어와 있는 적 목록. 여러 Collider를 가진 적도 하나로 집계한다.</summary>
        public IReadOnlyCollection<EnemyBase> Enemies => counts.Keys;
        private readonly Dictionary<Collider, EnemyBase> contacts = new();
        private readonly Dictionary<EnemyBase, int> counts = new();
        private readonly List<Collider> stale = new();
        private Action<EnemyBase> enter;
        private Action<EnemyBase> exit;
        private LayerMask layers;

        /// <summary>Prefab 인스턴스 크기를 설정한 뒤 호출한다. 설치 순간에는 Overlap, 이후에는 Enter/Exit를 전달한다.</summary>
        public void Initialize(LayerMask enemyLayers, Action<EnemyBase> onEnter, Action<EnemyBase> onExit)
        {
            layers = enemyLayers; enter = onEnter; exit = onExit;
            var sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;
            Physics.SyncTransforms();
            // 생성 시에만 Overlap. 이후에는 Enter/Exit만 사용한다.
            float radius = sphere.radius * Mathf.Abs(transform.lossyScale.x);
            foreach (var other in Physics.OverlapSphere(transform.TransformPoint(sphere.center), radius, layers,
                QueryTriggerInteraction.Collide)) OnTriggerEnter(other);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (enter == null || contacts.ContainsKey(other) || (layers.value & (1 << other.gameObject.layer)) == 0) return;
            var enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.Status == null || enemy.Status.HP <= 0) return;
            contacts.Add(other, enemy);
            counts.TryGetValue(enemy, out int count);
            counts[enemy] = count + 1;
            if (count == 0) enter(enemy);
        }
        private void OnTriggerExit(Collider other)
        {
            if (!contacts.TryGetValue(other, out var enemy)) return;
            contacts.Remove(other);
            if (--counts[enemy] > 0) return;
            counts.Remove(enemy);
            exit?.Invoke(enemy);
        }
        private void FixedUpdate()
        {
            // Collider 비활성화/파괴 시 Unity가 Exit를 생략하는 경우도 정리한다.
            stale.Clear();
            foreach (var pair in contacts)
                if (pair.Key == null || !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy ||
                    pair.Value == null || pair.Value.Status == null || pair.Value.Status.HP <= 0) stale.Add(pair.Key);
            foreach (var collider in stale) OnTriggerExit(collider);
        }
        private void OnDisable()
        {
            foreach (var enemy in new List<EnemyBase>(counts.Keys)) exit?.Invoke(enemy);
            contacts.Clear(); counts.Clear(); enter = null; exit = null;
        }
    }
}
