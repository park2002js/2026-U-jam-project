using UnityEngine;

public class BaseBuilding : MonoBehaviour
{
    // 외부에서 조작할 최대 체력
    [SerializeField] private int maxHealth = 1000;
    
    private int currentHealth;
    private bool isDestroyed = false;

    private void Start()
    {
        // 게임 시작 시 거점의 현재 체력을 설정된 최대 체력으로 초기화
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        // 피격 시 데미지만큼 체력을 차감하고 체력이 0 이하일 경우 게임 오버 처리를 호출
    }

    public void HealBase(int amount)
    {
        // 회복 요청 시 최대 체력을 초과하지 않는 범위 내에서 현재 체력을 증가
    }

    public int GetCurrentHealth()
    {
        // UI 등 외부 시스템에서 거점의 체력을 표시할 수 있도록 현재 체력 값을 반환
        return 0;
    }

}