using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>시체 폭발: 플레이어의 N번째 처치 위치에서 주변 적에게 한 번 피해를 준다.</summary>
    public sealed class CorpseExplosionEffect : ItemEffect
    {
        private readonly int kills;
        private readonly float radius, damagePercent;
        /// <summary>Catalog에서 처치 주기/반경/공격력 계수를 지정한다.</summary>
        public CorpseExplosionEffect(int kills, float radius, float damagePercent) { this.kills = kills; this.radius = radius; this.damagePercent = damagePercent; }
        /// <summary>EnemyKilled에서 호출한다. 카운트를 먼저 초기화해 폭발에 의한 처치도 다음 주기에 집계한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            if (c.Player.Increment(c.Item) < kills) return;
            c.Player.ResetCounter(c.Item);
            /* VFX: c.Position에 시체 폭발 표현을 추가한다. */
            foreach (var enemy in ItemWorld.Circle(c.Position, radius, c.EnemyMask)) c.Damage(enemy, damagePercent);
        }
    }
}
