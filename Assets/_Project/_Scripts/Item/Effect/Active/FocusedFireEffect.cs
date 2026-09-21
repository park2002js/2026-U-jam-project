using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>집중 사격: 시전 순간 원 안의 각 적에게 슈팅과 같은 직접 피해를 여러 번 준다. 슈팅 이벤트는 추가 발행하지 않는다.</summary>
    public sealed class FocusedFireEffect : ItemEffect
    {
        private readonly float radius, damagePercent;
        private readonly int shots;
        /// <summary>Catalog에서 반경/타격 횟수/타격당 공격력 계수를 지정한다.</summary>
        public FocusedFireEffect(float radius, int shots, float damagePercent) { this.radius = radius; this.shots = shots; this.damagePercent = damagePercent; }
        /// <summary>중앙 SkillUse가 호출한다. 모든 타격을 같은 프레임에 처리한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            /* VFX: 이 위치에 집중 사격의 총탄/적중 표현을 추가한다. */
            foreach (var enemy in enemies) for (int i = 0; i < shots; i++) c.Damage(enemy, damagePercent);
            c.ReportHits(enemies);
        }
    }
}
