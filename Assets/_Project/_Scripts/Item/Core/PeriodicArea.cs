using System;
using System.Collections;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>A형 장판: 설치 시와 이후 0.5초마다 OverlapSphere로 현재 적을 다시 수집한다. Collider를 생성하지 않는다.</summary>
    public static class PeriodicArea
    {
        /// <summary>새 A형 Effect의 Execute에서 context.Run(Run(...))으로 사용한다. tick에는 해당 아이템의 실제 효과만 전달한다.</summary>
        public static IEnumerator Run(ItemUseContext context, float radius, float duration, Action<IReadOnlyList<EnemyBase>> tick)
        {
            if (radius <= 0 || duration <= 0 || tick == null) throw new ArgumentException("장판 A 인자를 확인하세요.");
            float until = Time.time + duration;
            do
            {
                tick(ItemWorld.Circle(context.Position, radius, context.EnemyMask));
                yield return new WaitForSeconds(0.5f);
            } while (Time.time < until);
        }
    }
}
