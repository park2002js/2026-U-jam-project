using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>집념: 같은 적 연속 적중 임계치에 도달하면 공격속도 버프를 갱신한다.</summary>
    public sealed class TenacityEffect : ItemEffect
    {
        private readonly int hits;
        private readonly float percent, duration;
        /// <summary>Catalog에서 연속 횟수/공격속도 증가율/지속 시간을 지정한다.</summary>
        public TenacityEffect(int hits, float percent, float duration) { this.hits = hits; this.percent = percent; this.duration = duration; }
        /// <summary>매 Shooting 이벤트에서 호출한다. 빗나가거나 대상이 바뀌면 연속 판정이 초기화된다.</summary>
        public override void Execute(ItemUseContext c)
        {
            EnemyBase target = c.Enemies != null && c.Enemies.Count > 0 ? c.Enemies[0] : null;
            if (!c.Player.ConsecutiveHit(c.Item, target, hits)) return;
            BuffManager.Instance.SetTimed(c.Player, c.Item, BuffStat.AttackSpeed, percent, duration);
            /* VFX: 플레이어 공격속도 강화 표현을 이곳에 추가한다. */
        }
    }
}
