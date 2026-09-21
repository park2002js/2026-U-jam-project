using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>십자 폭격: 지연 후 필드 전체 직선 범위를 판정한다. 교차점의 적은 가로/세로 피해를 각각 받는다.</summary>
    public sealed class CrossBombardmentEffect : ItemEffect
    {
        private readonly float thickness, delay, damagePercent;
        /// <summary>Catalog에서 전체 선 폭/지연/공격력 계수를 정한다.</summary>
        public CrossBombardmentEffect(float thickness, float delay, float damagePercent) { this.thickness = thickness; this.delay = delay; this.damagePercent = damagePercent; }
        /// <summary>중앙 SkillUse가 호출한다. 시전 당시 좌표를 보존한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Bombard(c));
        private IEnumerator Bombard(ItemUseContext c)
        {
            /* VFX: 시전 당시 위치를 기준으로 폭격 예고 표현을 추가한다. */
            yield return new WaitForSeconds(delay);
            var horizontal = ItemWorld.Line(c.Position, thickness, true, c.EnemyMask);
            foreach (var enemy in horizontal) c.Damage(enemy, damagePercent);
            var vertical = ItemWorld.Line(c.Position, thickness, false, c.EnemyMask);
            foreach (var enemy in vertical) c.Damage(enemy, damagePercent);
            var hits = new HashSet<EnemyBase>(horizontal);
            hits.UnionWith(vertical);
            c.ReportHits(new List<EnemyBase>(hits));
            /* VFX: 실제 폭격 피해가 발생한 위치에 착탄 표현을 추가한다. */
        }
    }
}
