using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>중력장: 장판 B 안에 있는 동안에만 MovementSpeed 보정을 적용한다. Exit/장판 소멸 시 이 장판의 효과만 해제한다.</summary>
    public sealed class GravityFieldEffect : ItemEffect
    {
        private readonly float radius, percent, duration;
        private readonly string prefabPath;
        /// <summary>Catalog에서 반경/보정률/지속 시간/단위 Collider Prefab 경로를 정한다.</summary>
        public GravityFieldEffect(float radius, float percent, float duration, string prefabPath)
        { this.radius = radius; this.percent = percent; this.duration = duration; this.prefabPath = prefabPath; }
        /// <summary>중앙 SkillUse가 호출한다. 처음에는 Overlap, 이후에는 TriggerEnter/Exit로 판정한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            /* VFX: 중력장 장판 생성 표현을 추가한다. 지속 표현은 생성된 장판의 자식에 붙인다. */
            c.Run(ItemWorld.ConditionArea(c, prefabPath, radius, duration, BuffStat.MovementSpeed, -percent));
        }
    }
}
