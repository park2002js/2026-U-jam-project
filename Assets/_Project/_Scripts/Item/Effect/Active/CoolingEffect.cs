using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>냉각: 지연 후 원 안의 적에게 경직과 빙결을 동시에 적용한다.</summary>
    public sealed class CoolingEffect : ItemEffect
    {
        private readonly float radius, delay, stunDuration;
        /// <summary>Catalog에서 반경/시전 지연/경직 시간을 정한다.</summary>
        public CoolingEffect(float radius, float delay, float stunDuration) { this.radius = radius; this.delay = delay; this.stunDuration = stunDuration; }
        /// <summary>중앙 SkillUse가 호출한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Freeze(c));
        private IEnumerator Freeze(ItemUseContext c)
        {
            yield return new WaitForSeconds(delay);
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            /* VFX: 빙결 폭발과 경직 표현을 이곳에 추가한다. */
            foreach (var enemy in enemies)
            {
                BuffManager.Instance.SetTimed(enemy.Status, c.Item, BuffStat.Stun, 100, stunDuration);
                c.Element(enemy, ItemElement.Freeze);
            }
            c.ReportHits(enemies);
        }
    }
}
