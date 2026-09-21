using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>전류 탄환: 슈팅에 맞은 적에게 확률적으로 Shock 속성을 부여한다.</summary>
    public sealed class ElectricBulletEffect : ItemEffect
    {
        private readonly float chance;
        /// <summary>Catalog에서 0~100의 발동 확률을 지정한다.</summary>
        public ElectricBulletEffect(float chance) => this.chance = Mathf.Clamp(chance, 0, 100);
        /// <summary>매 Shooting 이벤트에서 호출한다. 미적중에는 아무 일도 하지 않는다.</summary>
        public override void Execute(ItemUseContext c)
        {
            if (c.Enemies == null) return;
            foreach (var enemy in c.Enemies)
            {
                if (Random.value * 100f >= chance) continue;
                c.Element(enemy, ItemElement.Shock);
                /* VFX: 전류 탄환의 속성 적중 표현을 이곳에 추가한다. */
            }
        }
    }
}
