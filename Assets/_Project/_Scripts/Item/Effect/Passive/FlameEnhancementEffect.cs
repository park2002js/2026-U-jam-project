using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>염화: 화상 공격력 계수에 퍼센트포인트를 더한다. 111%+20은 131%이며 별도 1.2배가 아니다.</summary>
    public sealed class FlameEnhancementEffect : ItemEffect
    {
        private readonly float percent;
        /// <summary>Catalog에서 화상 계수에 더할 퍼센트포인트를 지정한다.</summary>
        public FlameEnhancementEffect(float percent) => this.percent = percent;
        /// <summary>Equipped 이벤트에서 호출한다. Item.Unequip이 이 출처의 보정을 제거한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.BurnCoefficient, percent);
            /* VFX: 보유 상태를 보여주는 지속 표현이 필요하면 이곳에 추가한다. */
        }
    }
}
