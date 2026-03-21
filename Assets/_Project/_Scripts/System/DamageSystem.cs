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

    /// <summary>
    /// 필수 값만 넣으면 나머지는 기본값으로 채워주는 팩토리 메서드
    /// </summary>
    public static DamageInfo Default(float amount = 0f, float invincibleTime = 0f)
    {
        return new DamageInfo
        {
            Amount = amount,
            InvincibleTime = invincibleTime,
            BypassInvincibility = false,
            Type = DamageType.Normal,
            Reaction = HitReactionType.None,
            Instigator = null
        };
    }
}
#endregion

public class DamageSystem
{
    
}
