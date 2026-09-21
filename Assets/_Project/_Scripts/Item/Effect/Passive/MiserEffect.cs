using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>수전노: 보유 중 CurrencyGain 수치를 합연산으로 보정한다. 해제 시 기본 수치로 정확히 돌아간다.</summary>
    public sealed class MiserEffect : ItemEffect
    {
        private readonly float percent;
        /// <summary>Catalog에서 증가/감소 보정률을 지정한다.</summary>
        public MiserEffect(float percent) => this.percent = percent;
        /// <summary>Equipped 이벤트에서 호출한다. 조건형 보정의 해제는 Item.Unequip이 담당한다.</summary>
        public override void Execute(ItemUseContext c)
        {
            BuffManager.Instance.SetCondition(c.Player, c.Item, BuffStat.CurrencyGain, percent);
            /* VFX: 수전노 보유 상태 표현이 필요하면 이곳에 추가한다. */
        }
    }
}
