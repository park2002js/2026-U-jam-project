using UnityEngine;
using EnemySystem;
public class Golem : Enemy
{
    protected override void InitStatus()
    {
        // 덩치가 크니까 체력과 공격력을 높게 설정
        HP = 500f;
        moveSpeed = 3f;
        AD = 50;
        AS = 0.5f; // 공격은 느림

        // 사거리도 덩치에 맞게 조금 키울 수 있습니다.
        chaseRange = 12f;
        attackRange = 4f;
    }

}
