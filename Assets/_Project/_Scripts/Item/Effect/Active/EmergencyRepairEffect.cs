using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>긴급 수리: 최대 체력 비율 회복. 사용 횟수는 PlayerStatus에 저장하고 소진 시 이 보유 개체를 삭제한다.</summary>
    public sealed class EmergencyRepairEffect : ItemEffect
    {
        private readonly float healPercent;
        private readonly int uses;
        /// <summary>Catalog에서 최대 체력 회복률과 사용 횟수를 정한다.</summary>
        public EmergencyRepairEffect(float healPercent, int uses) { this.healPercent = healPercent; this.uses = uses; }
        /// <summary>중앙 SkillUse가 호출한다. 최대 체력 상태에서도 1회 사용으로 센다.</summary>
        public override void Execute(ItemUseContext c)
        {
            c.Player.Heal(c.Player.MaxHealth * healPercent / 100f);
            /* VFX: 거점 수리/회복 표현을 이곳에 추가한다. */
            if (c.Player.Increment(c.Item) >= uses) c.Item.Consume();
        }
    }
}
