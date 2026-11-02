using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyViewerTarget : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private float _maxHealth = 100000;
        public float HP { get; private set; }
        public float DamageTaken { get; private set; }
        public int HitCount { get; private set; }

        private void Awake() => ResetTarget();

        public void ResetTarget()
        {
            HP = _maxHealth;
            DamageTaken = 0;
            HitCount = 0;
        }

        public float TakeDamage(DamageInfo info)
        {
            if (!float.IsFinite(info.Damage) || info.Damage <= 0 || HP <= 0) return 0;
            float applied = Mathf.Min(HP, info.Damage);
            HP -= applied;
            DamageTaken += applied;
            HitCount++;
            return applied;
        }
    }
}
