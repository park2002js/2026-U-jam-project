using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>생명력 전환: 현재 체력에서 최대 체력의 일정 비율을 지불하고 일반 공격 Damage%를 일시 강화한다.</summary>
    public sealed class LifeConversionEffect : ItemEffect
    {
        private readonly float healthCost, damagePercent, duration;
        /// <summary>Catalog에서 체력 지불률/데미지 증가율/시간을 정한다.</summary>
        public LifeConversionEffect(float healthCost, float damagePercent, float duration)
        { this.healthCost = healthCost; this.damagePercent = damagePercent; this.duration = duration; }
        /// <summary>중앙 SkillUse가 호출한다. 지불로 사망할 수 있고 부활 패시브도 정상 작동한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            c.Player.TakeDamage(new UJam.Runtime.Combat.DamageInfo(c.Player.MaxHealth * healthCost / 100f, c.Item.ID));
            if (c.Player.CurrentHealth > 0) BuffManager.Instance.SetTimed(c.Player, c.Item, BuffStat.Damage, damagePercent, duration);
            /* VFX: 체력 지불과 데미지 강화 표현을 이곳에 추가한다. */
        }
    }
}
