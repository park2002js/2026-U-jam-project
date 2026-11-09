using System.Collections;
using UJam.Runtime.Systems;

namespace Ujam.Runtime.Item
{
    /// <summary>돌풍: 원 안의 적에게 바람을 부여한다. 속성 조합 동작은 ElementManager 확장 시 추가한다.</summary>
    public class GustEffect : ItemEffect
    {
        private readonly float radius, duration;
        /// <summary>Catalog에서 반경과 VFX 유지 시간을 정한다. 바람 속성 수명은 ElementManager가 관리한다.</summary>
        public GustEffect(float radius, float duration) { this.radius = radius; this.duration = duration; }
        /// <summary>중앙 SkillUse가 호출한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Blow(c));
        private IEnumerator Blow(ItemUseContext c)
        {
            yield return ItemVfx.Spawn(c, radius * ItemWorld.CellWidth * 2f, duration);
            if (!c.Item.IsEquipped) yield break;
            var enemies = ItemWorld.Circle(c.Position, radius, c.EnemyMask);
            foreach (var enemy in enemies) c.Element(enemy, ElementType.Wind);

            c.ReportHits(enemies);
        }
    }
}
