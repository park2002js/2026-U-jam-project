using UnityEngine;
using UJam.Runtime.Enemy;

namespace UJam.Runtime.Item
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/DamageOverTime")]
    public sealed class DamageOverTimeEffect : ItemEffect
    {
        [SerializeField] private float damagePerTick = 5f;

        public override void Tick(ItemUseContext context)
        {
            if (context.Target == null) return;

            EnemyStatus status = context.Target.GetComponent<EnemyStatus>();
            if (status == null)
            {
                Debug.Log("[DoT] EnemyStatus 없음");
                return;
            }

            status.ApplyDamage(damagePerTick);
        }
    }
}