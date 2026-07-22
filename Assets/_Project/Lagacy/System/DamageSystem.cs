using UnityEngine;

// 데미지 시스템 공용 인터페이스 및 구조체 정의
#region [ Core Damage System ]

public enum HitReactionType { None, Flinch, Knockdown }
// 기존 시스템에서 사용하는 피해 종류
public enum LegacyDamageType { Normal, Pierce, Fire }
/// <summary>
/// 이 인터페이스를 가진 객체는 범용적인 데미지 시스템의 대상이 됩니다. (플레이어, 적, 거점 등)
/// </summary>
// 기존 피해 처리기를 위한 수신 계약
public interface ILegacyDamageable
{
    void TakeDamage(LegacyDamageInfo info);
}

/// <summary>
/// Runtime Combat으로 이전되기 전 기존 공격의 명세서 역할을 하는 구조체입니다.
/// </summary>
public struct LegacyDamageInfo
{
    public float Amount;             // 데미지 수치
    public float InvincibleTime;     // 부여할 무적 시간
    public bool BypassInvincibility; // 무적 관통 여부

    // [Tech Debt] 추후 확장될 시스템을 위한 변수 (현재는 기본값 처리)
    // 기존 시스템의 피해 종류
    public LegacyDamageType Type;
    public HitReactionType Reaction;
    public GameObject Instigator;
    public Element Element;

    /// <summary>
    /// 필수 값만 넣으면 나머지는 기본값으로 채워주는 팩토리 메서드
    /// </summary>
    // 기존 호출자가 사용하는 기본 피해 정보 생성
    public static LegacyDamageInfo Default(float amount = 0f, float invincibleTime = 0f, Element element = null)
    {
        // 입력값과 기존 기본 정책을 담은 피해 정보 반환
        return new LegacyDamageInfo
        {
            Amount = amount,
            InvincibleTime = invincibleTime,
            BypassInvincibility = false,
            Type = LegacyDamageType.Normal,
            Reaction = HitReactionType.None,
            Instigator = null,
            Element = element
        };
    }
}
#endregion

public class DamageSystem
{
    // 기존 적과 속성 수신기에 피해 정보 전달
    public static void ApplyDamage(GameObject targetObj, LegacyDamageInfo info)
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
