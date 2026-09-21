using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>생명력 흡수: 슈팅의 기본 피해와 부가 효과 처리가 끝난 뒤 실제 피해 일부를 회복한다. 과잉 피해는 포함하지 않는다.</summary>
    public sealed class LifeStealEffect : ItemEffect
    {
        private readonly float percent;
        /// <summary>Catalog에서 실제 피해 대비 회복 비율을 지정한다.</summary>
        public LifeStealEffect(float percent) => this.percent = percent;
        /// <summary>ShootingResolved에서 호출한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            c.Player.Heal(c.Signal.AppliedDamage * percent / 100f);
            /* VFX: 적중 위치에서 플레이어로 향하는 흡수 표현을 이곳에 추가한다. */
        }
    }
}
