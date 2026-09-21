using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>리저렉션: 사망 직전 피해를 한 번 막고 회복한 후 이 아이템 개체를 소모한다.</summary>
    public sealed class ResurrectionEffect : ItemEffect
    {
        private readonly float healPercent;
        /// <summary>Catalog에서 부활 시 최대 체력 회복 비율을 지정한다.</summary>
        public ResurrectionEffect(float healPercent) => this.healPercent = healPercent;
        /// <summary>BeforeDeath에서만 호출한다. 먼저 성공한 한 개체만 소모된다.</summary>
        public override void Execute(ItemUseContext c)
        {
            if (!c.Player.Revive(healPercent)) return;
            c.Signal.DeathPrevented = true;
            /* VFX: 거점 부활/보호 표현을 이곳에 추가한다. */
            c.Item.Consume();
        }
    }
}
