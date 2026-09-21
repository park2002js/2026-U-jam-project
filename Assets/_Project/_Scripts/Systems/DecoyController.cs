using System;
using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace UJam.Runtime.Systems
{
    /// <summary>미끼 자체의 체력/수명/도발을 관리한다. ItemEffect는 위치/체력/수명만 정하고 이 컴포넌트에 맡긴다.</summary>
    [RequireComponent(typeof(AreaTrigger))]
    public sealed class DecoyController : MonoBehaviour, IDamageable
    {
        private float maxHealth, health, duration;
        private LayerMask enemyMask;
        private bool initialized;
        private Action<EnemyBase> onTaunt;
        private readonly HashSet<EnemyBase> taunted = new();
        public float Health => health;

        /// <summary>비활성 인스턴스에 설정한 뒤 SetActive(true) 한다. OnEnable이 초기 Overlap과 진입 구독을 시작한다.</summary>
        public void Initialize(float hitPoints, float lifetime, LayerMask mask, Action<EnemyBase> onEnter)
        {
            maxHealth = Mathf.Max(1, hitPoints); duration = Mathf.Max(0.01f, lifetime);
            enemyMask = mask; onTaunt = onEnter; initialized = true;
        }
        private void OnEnable()
        {
            if (!initialized) return;
            health = maxHealth;
            GetComponent<AreaTrigger>().Initialize(enemyMask, Taunt, Release);
        }
        private void Update()
        {
            if (!initialized) return;
            health = Mathf.Max(0, health - maxHealth / duration * Time.deltaTime);
            if (health <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
        }
        private void Taunt(EnemyBase enemy)
        {
            if (enemy.FSM == null || !taunted.Add(enemy)) return;
            enemy.FSM.Targets.Remove(gameObject);
            enemy.FSM.Targets.Add(gameObject);
            enemy.ReTargeting();
            onTaunt?.Invoke(enemy);
        }
        private void Release(EnemyBase enemy)
        {
            taunted.Remove(enemy);
            if (enemy == null || enemy.FSM == null) return;
            enemy.FSM.Targets.Remove(gameObject);
            if (enemy.Status.HP > 0 && enemy.FSM.Targets.Count > 0) enemy.ReTargeting();
        }
        private void OnDisable()
        {
            foreach (var enemy in new List<EnemyBase>(taunted)) Release(enemy);
        }
        /// <summary>적 공격을 받으면 실제 감소량을 반환하고, 체력이 소진되면 도발도 해제한다.</summary>
        public float TakeDamage(DamageInfo info)
        {
            if (!float.IsFinite(info.Damage) || info.Damage <= 0 || health <= 0) return 0;
            float applied = Mathf.Min(health, info.Damage);
            health -= applied;
            if (health <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
            return applied;
        }
    }
}
