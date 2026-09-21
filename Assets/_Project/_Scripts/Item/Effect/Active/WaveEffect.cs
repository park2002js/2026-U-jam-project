using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Systems;
using UJam.Runtime.Enemy;
using UnityEngine;
using ItemElement = UJam.Runtime.Systems.ElementType;

namespace Ujam.Runtime.Item
{
    /// <summary>파도: 얇은 직사각형을 +Z로 이동시키며 각 적을 한 번만 밀어내고 빙결을 부여한다.</summary>
    public sealed class WaveEffect : ItemEffect
    {
        private readonly float width, depth, distance, duration, interval, push;
        /// <summary>Catalog에서 판정 폭/두께/이동 거리/수명/검사 간격/밀기 거리를 정한다.</summary>
        public WaveEffect(float width, float depth, float distance, float duration, float interval, float push)
        { this.width = width; this.depth = depth; this.distance = distance; this.duration = duration; this.interval = interval; this.push = push; }
        /// <summary>중앙 SkillUse가 호출한다.</summary>
        public override void Execute(ItemUseContext c) => c.Run(Move(c));
        private IEnumerator Move(ItemUseContext c)
        {
            var affected = new HashSet<EnemyBase>();
            float started = Time.time;
            /* VFX: 파도 모델을 생성하고 아래 center 좌표와 함께 이동시킨다. c.Item.Runtime.Track으로 수명을 등록한다. */
            while (Time.time - started < duration)
            {
                Vector3 center = c.Position + Vector3.forward * (distance * ItemWorld.CellHeight * (Time.time - started) / duration);
                var newHits = new List<EnemyBase>();
                foreach (var enemy in ItemWorld.Box(center, width, depth, c.EnemyMask))
                {
                    if (!affected.Add(enemy)) continue;
                    enemy.Knockback(Vector3.forward * (push * ItemWorld.CellHeight));
                    c.Element(enemy, ItemElement.Freeze);
                    newHits.Add(enemy);
                }
                c.ReportHits(newHits);
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
