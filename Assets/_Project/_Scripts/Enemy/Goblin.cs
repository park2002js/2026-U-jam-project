using UnityEngine;

public class Goblin : Enemy
{
    protected override void InitStatus()
    {
        // 체력은 낮지만 속도는 매우 빠르게 설정
        hp = 100f;
        moveSpeed = 8.0f;
        ad = 15;
        attackSpeed = 2.0f; // 공격은 아주 빠름

        chaseRange = 15f;
        attackRange = 3f;
    }
}