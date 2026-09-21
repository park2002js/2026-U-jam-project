using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>화염구: 지정 지연 후 원형 범위를 조회하고 피해를 먼저 준 다음 화상을 부여한다.</summary>
    public sealed class FireballEffect : ItemEffect
    {
        private readonly float radius, damagePercent, delay;
        /// <summary>Catalog에서 반경/공격력 계수/지연 시간을 정한다.</summary>
        public FireballEffect(float radius, float damagePercent, float delay) { this.radius = radius; this.damagePercent = damagePercent; this.delay = delay; }
        /// <summary>중앙 SkillUse가 호출한다. 시전 위치는 지연 중에도 변하지 않는다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Strike(c));
        private IEnumerator Strike(ItemUseContext c)
        {
            /* VFX: 낙하 예고나 화염구 투사체를 이곳에서 시작한다. */
            yield return new WaitForSeconds(delay);
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            foreach (var enemy in enemies) { c.Damage(enemy, damagePercent); c.Element(enemy, ItemElement.Burn); }
            /* VFX: 착탄/화염 폭발 표현을 이곳에 추가한다. */
            c.ReportHits(enemies);
        }
    }
}
