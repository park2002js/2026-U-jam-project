using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>아드레날린: 일정 시간 공격력/공격속도를 높인다. 같은 아이템 출처는 중첩 대신 갱신된다.</summary>
    public sealed class AdrenalineEffect : ItemEffect
    {
        private readonly float attack, speed, duration;
        /// <summary>Catalog에서 공격력/공격속도 증가율과 시간을 정한다.</summary>
        public AdrenalineEffect(float attack, float speed, float duration) { this.attack = attack; this.speed = speed; this.duration = duration; }
        /// <summary>중앙 SkillUse가 호출한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            BuffManager.Instance.SetTimed(c.Player, c.Item, BuffStat.AttackDamage, attack, duration);
            BuffManager.Instance.SetTimed(c.Player, c.Item, BuffStat.AttackSpeed, speed, duration);
            /* VFX: 플레이어 강화 표현을 추가한다. 지속 시간은 BuffManager와 맞춘다. */
        }
    }
}
