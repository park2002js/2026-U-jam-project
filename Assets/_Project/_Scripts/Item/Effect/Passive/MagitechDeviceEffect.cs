using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>마공학 장치: 보유 중 CooldownReduction 수치를 합연산으로 보정한다. 해제 시 기본 수치로 정확히 돌아간다.</summary>
    public sealed class MagitechDeviceEffect : ItemEffect
    {
        private readonly float percent;
        /// <summary>Catalog에서 증가/감소 보정률을 지정한다.</summary>
        public MagitechDeviceEffect(float percent) => this.percent = percent;
        /// <summary>Equipped 이벤트에서 호출한다. 조건형 보정의 해제는 Item.Unequip이 담당한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.CooldownReduction, percent);
            /* VFX: 마공학 장치 보유 상태 표현이 필요하면 이곳에 추가한다. */
        }
    }
}
