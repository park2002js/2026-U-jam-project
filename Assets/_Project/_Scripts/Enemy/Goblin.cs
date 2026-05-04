using UnityEngine;
using EnemySystem;
public class Goblin : Enemy
{
    protected override void InitStatus()
    {
        // 체력은 낮지만 속도는 매우 빠르게 설정
        HP = 100f;
        moveSpeed = 8.0f;
        AD = 15;
        AS = 2.0f; // 공격은 아주 빠름

        chaseRange = 15f;
        attackRange = 3f;
    }
}