using UnityEngine;

// 데미지 시스템 공용 인터페이스 및 구조체 정의
#region [ Core Damage System ]

public enum HitReactionType { None, Flinch, Knockdown }
public enum DamageType { Normal, Pierce, Fire }
/// <summary>
/// 이 인터페이스를 가진 객체는 범용적인 데미지 시스템의 대상이 됩니다. (플레이어, 적, 거점 등)
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}

/// <summary>
/// 모든 공격의 명세서 역할을 하는 구조체입니다.
/// </summary>
public struct DamageInfo
{
    public float Amount;             // 데미지 수치
    public float InvincibleTime;     // 부여할 무적 시간
    public bool BypassInvincibility; // 무적 관통 여부

    // [Tech Debt] 추후 확장될 시스템을 위한 변수 (현재는 기본값 처리)
    public DamageType Type;
    public HitReactionType Reaction;
    public GameObject Instigator;
    public Element Element;

    /// <summary>
    /// 필수 값만 넣으면 나머지는 기본값으로 채워주는 팩토리 메서드
    /// </summary>
    public static DamageInfo Default(float amount = 0f, float invincibleTime = 0f, Element element = null)
    {
        return new DamageInfo
        {
            Amount = amount,
            InvincibleTime = invincibleTime,
            BypassInvincibility = false,
            Type = DamageType.Normal,
            Reaction = HitReactionType.None,
            Instigator = null,
            Element = element
        };
    }
}
#endregion

public class DamageSystem
{
    public static void ApplyDamage(GameObject targetObj, DamageInfo info)
    {
        // 1. 순수 물리 데미지 처리 (Enemy.cs의 TakeDamage 호출)
        EnemySystem.Enemy enemy = targetObj.GetComponent<EnemySystem.Enemy>();
        if (enemy != null && info.Amount > 0)
        {
            enemy.TakeDamage(info.Amount);
        }

        // 2. 속성 부여 처리 (ElementReceiver 호출)
        if (info.Element != null)
        {
            ElementReceiver receiver = targetObj.GetComponent<ElementReceiver>();
            if (receiver != null)
            {
                receiver.ApplyElement(info);
            }
        }
    }
    
}
