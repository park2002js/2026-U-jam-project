using System;
using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>Prefab의 체력/수명/도발 반경으로 배치 즉시 동작하고, 소멸 시 모든 유인 대상에게 알린다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AreaTrigger))]
    public class DecoyController : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(.01f)] private float duration = 20f;
        [SerializeField, Min(.01f), Tooltip("도발 Trigger의 반경. 루트 Scale이 1일 때 월드 단위.")]
        private float tauntRadius = 6f;
        [SerializeField] private LayerMask enemyMask = ~0;
        private float health, remainingLifetime;
        private readonly HashSet<EnemyBase> taunted = new();
        public float Health => health;
        public event Action<EnemyBase> Taunted;

        private void OnEnable()
        {
            health = Mathf.Max(1f, maxHealth);
            remainingLifetime = Mathf.Max(.01f, duration);
            GetComponent<SphereCollider>().radius = Mathf.Max(.01f, tauntRadius);
            var area = GetComponent<AreaTrigger>();
            area.enabled = true;
            // Overlap과 이후 Trigger 진입은 같은 Taunt를 호출한다. 이탈해도 소멸 전까지 우선순위를 유지한다.
            area.Initialize(enemyMask, Taunt, null);
        }
        private void Update()
        {
            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0) Despawn();
        }
        private void Taunt(EnemyBase enemy)
        {
            if (!isActiveAndEnabled || health <= 0 || enemy == null || enemy.FSM == null) return;

            taunted.Add(enemy);
            enemy.FSM.Targets.Remove(gameObject);
            enemy.FSM.Targets.Add(gameObject);
            enemy.ReTargeting();

            Taunted?.Invoke(enemy);
        }
        private void Release(EnemyBase enemy)
        {
            taunted.Remove(enemy);
            if (enemy == null || enemy.FSM == null) return;

            if (enemy.FSM.Targets.Remove(gameObject) && enemy.Status != null && enemy.Status.HP > 0)
                enemy.ReTargeting();
        }
        private void OnDisable()
        {
            health = 0;
            GetComponent<AreaTrigger>().enabled = false;
            foreach (var enemy in new List<EnemyBase>(taunted)) Release(enemy);
        }
        private void Despawn()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        /// <summary>적 공격을 받으면 실제 감소량을 반환하고, 체력이 소진되면 도발도 해제한다.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (!isActiveAndEnabled || !float.IsFinite(info.Damage) || info.Damage <= 0 || health <= 0) return 0;

            float applied = Mathf.Min(health, info.Damage);
            health -= applied;

            if (health <= 0) Despawn();
            return applied;
        }
    }
}
