using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Grid;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>가로 폭격. 낙하 순서와 무관하게 delay에 모든 폭발·OverlapBox 피해를 함께 적용한다.</summary>
    public class HorizontalBombardmentEffect : ItemEffect
    {
        private readonly float thickness, delay, damagePercent, fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight;
        public HorizontalBombardmentEffect(float thickness, float delay, float damagePercent, float fallDuration = .3f,
            float sweepDuration = .25f, float incomingSpeed = 1f, float impactSpeed = 1f, float impactLifetime = 1.5f, float spawnHeight = 10f)
        {
            Validate(thickness, delay, damagePercent, fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight);
            this.thickness = thickness; this.delay = delay; this.damagePercent = damagePercent;
            this.fallDuration = fallDuration; this.sweepDuration = sweepDuration; this.incomingSpeed = incomingSpeed;
            this.impactSpeed = impactSpeed; this.impactLifetime = impactLifetime; this.spawnHeight = spawnHeight;
        }
        public override void Execute(ItemUseContext c) => c.Run(Bombard(c, thickness, 0, false, delay, damagePercent,
            fallDuration, sweepDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight));

        internal static IEnumerator Bombard(ItemUseContext c, float thickness, float length, bool cross, float delay, float damagePercent,
            float fallDuration, float sweepDuration, float incomingSpeed, float impactSpeed, float impactLifetime, float spawnHeight)
        {
            float castAt = Time.time;
            var areas = Areas(c.Position, thickness, length, cross, c.Item.Meta.Preview.Direction);
            ItemBombardmentVfx visual = null;
            yield return ItemBombardmentVfx.Create(c, value => visual = value);
            if (!c.Item.IsEquipped) yield break;
            if (visual != null) visual.Begin(areas, spawnHeight, fallDuration, sweepDuration, incomingSpeed);
            float remaining = delay - (Time.time - castAt);
            if (remaining > 0) yield return new WaitForSeconds(remaining);
            if (!c.Item.IsEquipped) yield break;
            if (visual != null) visual.Impact(impactSpeed, impactLifetime);
            var hits = new HashSet<EnemyBase>();
            foreach (var area in areas)
            {
                var enemies = ItemWorld.Box(area.Center, area.Size, area.Rotation, c.EnemyMask);
                foreach (var enemy in enemies) { c.Damage(enemy, damagePercent); hits.Add(enemy); }
            }
            c.ReportHits(new List<EnemyBase>(hits));
        }

        public static ItemBombardmentArea[] Areas(Vector3 cursor, float thickness, float length, bool cross, Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 center = cursor;
            float width = length * ItemWorld.CellWidth;
            if (!cross)
            {
                var grid = GridSystem.Instance;
                bool valid = grid != null && grid.IsInitialized;
                float columns = valid ? grid.ColumnCount : 32, rows = valid ? grid.RowCount : 32;
                Vector3 origin = valid ? grid.Origin : new Vector3(-15.5f, 0, -15.5f);
                Vector3 fieldCenter = origin + new Vector3((columns - 1) * ItemWorld.CellWidth / 2, 0, (rows - 1) * ItemWorld.CellHeight / 2);
                Vector3 right = rotation * Vector3.right;
                center += right * Vector3.Dot(fieldCenter - cursor, right);
                width = Mathf.Abs(right.x) * columns * ItemWorld.CellWidth + Mathf.Abs(right.z) * rows * ItemWorld.CellHeight;
            }
            var horizontal = new ItemBombardmentArea(center, new Vector3(width, 200, thickness * ItemWorld.CellHeight), rotation);
            if (!cross) return new[] { horizontal };
            return new[] { horizontal, new ItemBombardmentArea(cursor, new Vector3(thickness * ItemWorld.CellWidth, 200, length * ItemWorld.CellHeight), rotation) };
        }

        internal static void Validate(float thickness, float delay, float damagePercent, float fallDuration, float sweepDuration,
            float incomingSpeed, float impactSpeed, float impactLifetime, float spawnHeight)
        {
            foreach (float value in new[] { thickness, delay, damagePercent, fallDuration, incomingSpeed, impactSpeed, impactLifetime, spawnHeight })
                if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "폭격 설정은 유한한 양수여야 합니다.");
            if (!float.IsFinite(sweepDuration) || sweepDuration < 0 || fallDuration + sweepDuration > delay)
                throw new ArgumentOutOfRangeException(nameof(sweepDuration), "폭탄 낙하와 훑기 시간의 합은 피해 지연 이하여야 합니다.");
        }
    }
}
