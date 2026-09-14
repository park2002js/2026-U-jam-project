using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    public static class PeriodicArea
    {
        // 장판 A. Effect에서 runtime.Run(PeriodicArea.Run(...))으로 사용하며 Collider를 만들지 않는다.
        public static IEnumerator Run(Vector3 position, float radius, float duration, float interval,
            LayerMask layers, Action<IReadOnlyList<EnemyBase>> apply)
        {
            if (!float.IsFinite(radius) || radius <= 0 || !float.IsFinite(duration) || duration <= 0 ||
                !float.IsFinite(interval) || interval <= 0 || apply == null) throw new ArgumentException("장판 A 인자 오류");
            float until = Time.time + duration;
            do
            {
                apply(Overlap(position, radius, layers));
                yield return new WaitForSeconds(interval);
            } while (Time.time < until);
        }
        public static List<EnemyBase> Overlap(Vector3 position, float radius, LayerMask layers)
        {
            Physics.SyncTransforms();
            var enemies = new HashSet<EnemyBase>();
            foreach (var hit in Physics.OverlapSphere(position, radius, layers, QueryTriggerInteraction.Collide))
            {
                var enemy = hit.GetComponentInParent<EnemyBase>();
                if (enemy != null && enemy.Status != null && enemy.Status.HP > 0) enemies.Add(enemy);
            }
            return new List<EnemyBase>(enemies);
        }
    }
}
