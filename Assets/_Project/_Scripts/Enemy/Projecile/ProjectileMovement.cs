using System.Collections;
using UnityEngine;

namespace UJam.Runtime.Enemy.Projectiles
{
    public abstract class ProjectileMovement : ScriptableObject
    {
        /// <summary>
        /// 날려보낼 투사체의 운동을 세부 구현하도록 하는 코루틴화 된 함수이다.
        /// </summary>
        /// <param name="projectile"> 계속해서 위치를 갱신시킬, 날려보낼 투사체의 좌표 정보이다. 이미 RangedEnemy에서 시작좌표로 초기화된 상태이다. </param>
        /// <param name="destination"> 날려보낼 투사체의 도착 좌표 </param>
        /// <param name="speed"> 투사체 속도 </param>
        public abstract IEnumerator Move(Transform projectile, Vector3 destination, float speed);
    }
}
