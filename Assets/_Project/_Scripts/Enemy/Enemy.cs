using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float currentHp = 100f; // 적의 현재 체력

    // 투사체가 명중했을 때 호출할 데미지 함수
    public void takeDamage(float damage)
    {
        currentHp -= damage;
        Debug.Log($"{gameObject.name}가 {damage} 데미지를 받음! 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 처치됨!");
        // 적이 죽었을 때 오브젝트 삭제
        Destroy(gameObject); 
    }
}