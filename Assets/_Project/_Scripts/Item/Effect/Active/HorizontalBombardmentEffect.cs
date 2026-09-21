using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>가로 폭격: 지연 후 필드 전체 직선 범위를 판정한다.</summary>
    public sealed class HorizontalBombardmentEffect : ItemEffect
    {
        private readonly float thickness, delay, damagePercent;
        /// <summary>Catalog에서 전체 선 폭/지연/공격력 계수를 정한다.</summary>
        public HorizontalBombardmentEffect(float thickness, float delay, float damagePercent) { this.thickness = thickness; this.delay = delay; this.damagePercent = damagePercent; }
        /// <summary>중앙 SkillUse가 호출한다. 시전 당시 좌표를 보존한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Bombard(c));
        private IEnumerator Bombard(ItemUseContext c)
        {
            /* VFX: 시전 당시 위치를 기준으로 폭격 예고 표현을 추가한다. */
            yield return new WaitForSeconds(delay);
            var horizontal = ItemWorld.Line(c.Position, thickness, true, c.EnemyMask);
            foreach (var enemy in horizontal) c.Damage(enemy, damagePercent);
            c.ReportHits(horizontal);
            /* VFX: 실제 폭격 피해가 발생한 위치에 착탄 표현을 추가한다. */
        }
    }
}
