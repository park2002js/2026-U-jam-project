using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>돌풍: 원 안의 적에게 바람을 부여한다. 속성 조합 동작은 ElementManager 확장 시 추가한다.</summary>
    public sealed class GustEffect : ItemEffect
    {
        private readonly float radius;
        /// <summary>Catalog에서 반경을 정한다.</summary>
        public GustEffect(float radius) => this.radius = radius;
        /// <summary>중앙 SkillUse가 호출한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            /* VFX: 돌풍 표현을 이곳에 추가한다. */
            foreach (var enemy in enemies) c.Element(enemy, ItemElement.Wind);
            c.ReportHits(enemies);
        }
    }
}
