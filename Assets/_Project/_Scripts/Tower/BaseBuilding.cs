using UnityEngine;

public class BaseBuilding : MonoBehaviour, IDamageable
{
    [Header("Base Stats")]
    [SerializeField] private int maxHealth = 1000;
    [SerializeField] private int defense = 0; // 🌟 새로 추가된 방어력 스탯
    
    private int currentHealth;
    private bool isDestroyed = false;

    private void Start()
    {
        currentHealth = maxHealth;
        Debug.Log($"[거점] 방어 준비 완료! 체력: {currentHealth}/{maxHealth}, 방어력: {defense}");
    }

    // ==============================================================
    // 🌟 데미지 처리 구역
    // ==============================================================
    public void TakeDamage(DamageInfo info)
    {
        if (isDestroyed) return;

        // 1. 소수점 데미지를 정수로 변환
        int rawDamage = Mathf.RoundToInt(info.Amount);
        
        // 🌟 2. 방어력 적용: 원래 데미지에서 방어력을 뺍니다. (단, 최소 1의 데미지는 들어가도록 방어)
        int appliedDamage = Mathf.Max(1, rawDamage - defense);
        
        currentHealth -= appliedDamage;

        Debug.Log($"[거점] 공격받았습니다! (원래 데미지: {rawDamage} -> 방어력 적용: {appliedDamage}), 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0; 
            TriggerGameOver();
        }
    }

    public void TakeDamage(int damage)
    {
        DamageInfo info = DamageInfo.Default((float)damage);
        TakeDamage(info); 
    }

    public void TakeDamage(float damage)
    {
        DamageInfo info = DamageInfo.Default(damage);
        TakeDamage(info); 
    }

    // ==============================================================
    // 🌟 상점 강화(업그레이드) 구역
    // ==============================================================
    
    // 1번: 최대 체력 100 증가
    public void UpgradeMaxHealth()
    {
        int upgradeAmount = 100;
        maxHealth += upgradeAmount;
        currentHealth += upgradeAmount; // 최대 체력이 늘어난 만큼 현재 체력도 보너스로 채워줍니다!
        
        Debug.Log($"[거점 강화] 최대 체력이 증가했습니다! 현재 체력: {currentHealth}/{maxHealth}");
    }

    // 2번: 방어력 1 증가
    public void UpgradeDefense()
    {
        int upgradeAmount = 1;
        defense += upgradeAmount;
        
        Debug.Log($"[거점 강화] 방어력이 증가했습니다! 현재 방어력: {defense}");
    }

    // ==============================================================

    public void HealBase(int amount)
    {
        if (isDestroyed) return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    private void TriggerGameOver()
    {
        isDestroyed = true;
        Debug.LogError("거점이 파괴되었습니다! GameEndManager를 호출합니다.");

        if (GameEndManager.Instance != null)
        {
            GameEndManager.Instance.TriggerGameEnd(GameEndReason.NexusDestroyed);
        }
    }
}