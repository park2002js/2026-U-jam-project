using UnityEngine;

namespace EnemySystem
{
    public class Archer : Enemy
    {
        protected override void InitStatus()
        {
            HP = 50f;
            moveSpeed = 3.5f;
            AD = 10;
            AS = 1.0f; // 초당 1번 공격
            attackRange = 12f; // 원거리 적이므로 크게 설정
            chaseRange = 20f;
        }
    }
}