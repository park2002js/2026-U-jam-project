using UnityEngine;

namespace UJam.Runtime.Enemy.Movement
{
    public abstract class EnemyMovement : MonoBehaviour
    {
        protected EnemyBase _enemyBase;

        public void init(EnemyBase enemy)
        {
            _enemyBase = enemy;
        }
        
        /// <summary>
        /// Enemy가 Move 상태에 진입했을 때, 실제 이동을 구현하기 위해 호출하는 함수
        /// </summary>
        public virtual void Enter() {}

        /// <summary>
        /// Enemy가 Move 상태인 동안 반복 호출되는 함수
        /// 이동을 실제로 구현해야 한다.
        /// </summary>
        public virtual void Tick() {}

        /// <summary>
        /// Enemy가 Move 상태에서 벗어났을 때, 이동관련 로직을 마무리하기 위해 호출하는 함수
        /// </summary>
        public virtual void Exit() {}
    }
}