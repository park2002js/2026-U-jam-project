using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어의 권한을 비트 연산으로 제어하기 위한 Flags Enum
[Flags]
public enum PlayerLockFlags
{
    None   = 0,
    Move   = 1 << 0, // 1 (0001)
    Look   = 1 << 1, // 2 (0010)
    Action = 1 << 2, // 4 (0100) : 조준, 대쉬, 구르기 등 모든 스킬
    Damage = 1 << 3, // 8 (1000) : 피격 판정 (무적화)
    All    = ~0      // 모든 비트 1
}

/// <summary>
/// 플레이어의 모든 스탯(이동 속도, 체력 등)을 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class PlayerStatManager : MonoBehaviour
{
    public static PlayerStatManager Instance { get; private set; }

    #region [ Lock System (잠금 중첩 제어) ]
    // [ 추가됨 ] 누가(object) 어떤 권한(Flags)을 잠갔는지 추적하는 딕셔너리
    private Dictionary<object, PlayerLockFlags> lockSources = new Dictionary<object, PlayerLockFlags>();
    
    [Header("현재 잠금 상태 (디버그용)")]
    [SerializeField] private PlayerLockFlags currentLocks = PlayerLockFlags.None;

    /// <summary>
    /// 외부 시스템(기절, 컷신, 게임오버 등)이 플레이어의 특정 권한을 잠글 때 호출합니다.
    /// </summary>
    public void AddLock(object source, PlayerLockFlags flags)
    {
        if (lockSources.ContainsKey(source)) lockSources[source] = flags;
        else lockSources.Add(source, flags);
            
        RecalculateLocks();
    }

    /// <summary>
    /// 외부 시스템이 자신의 잠금을 해제할 때 호출합니다.
    /// </summary>
    public void RemoveLock(object source)
    {
        if (lockSources.ContainsKey(source))
        {
            lockSources.Remove(source);
            RecalculateLocks();
        }
    }

    private void RecalculateLocks()
    {
        currentLocks = PlayerLockFlags.None;
        foreach (var lockFlag in lockSources.Values)
        {
            currentLocks |= lockFlag; // 비트 OR 연산으로 모든 잠금 소스의 권한을 합침
        }
    }

    /// <summary>
    /// 특정 권한이 현재 잠겨있는지 확인합니다.
    /// </summary>
    public bool HasLock(PlayerLockFlags flagToCheck)
    {
        // AND 연산 결과가 0이 아니면, 검사하려는 비트 중 하나라도 잠겨있다는 뜻입니다.
        return (currentLocks & flagToCheck) != 0;
    }
    #endregion
    

    #region [ Core Stats ]
    // 체력 및 무적 관리 변수
    [Header("체력(Health) 설정")]
    [Tooltip("플레이어의 최대 체력")]
    [SerializeField] private float maxHealth = 1000f;
    [Tooltip("플레이어의 현재 체력 (디버그/인스펙터 확인용)")]
    [SerializeField] private float currentHealth = 1000f;

    [Header("무적(Invincibility) 설정")]
    [Tooltip("현재 무적 상태 여부")]
    [SerializeField] private bool isInvincible = false;
    [Tooltip("남은 무적 시간")]
    [SerializeField] private float invincibleTimer = 0f;

    [Header("이동 관련 스탯")]
    [Tooltip("기본 이동 속도")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [Tooltip("조준 상태일 때 적용되는 이동 속도")]
    [SerializeField] private float aimMoveSpeed = 5f;

    [Header("조준(Aim) 설정")]
    [Tooltip("현재 플레이어가 조준 상태인지 나타냅니다.")]
    [SerializeField] private bool isAiming = false;

    [Header("대쉬(Sprint) 설정")]
    [Tooltip("대쉬 시 적용될 이동 속도 배율 (기본 1.5배)")]
    [SerializeField] private float dashSpeedMultiplier = 1.5f;
    [Tooltip("현재 플레이어가 대쉬 상태인지 나타냅니다. (디버그 확인용)")]
    [SerializeField] private bool isDashing = false;

    // 신규: 구르기 관련 설정
    [Header("구르기(Roll) 설정")]
    [Tooltip("구르기 시 이동할 거리 (m)")]
    [SerializeField] private float rollDistance = 10.0f;
    [Tooltip("구르기 동작이 완료되는데 걸리는 시간 (초)")]
    [SerializeField] private float rollDuration = 0.5f;
    [Tooltip("구르기 재사용 대기시간 (초)")]
    [SerializeField] private float rollCooldown = 1.5f;
    
    [Tooltip("현재 플레이어가 구르기 상태인지 나타냅니다. (디버그 확인용)")]
    [SerializeField] private bool isRolling = false;
    private float lastRollTime = -999f; // 마지막으로 구르기를 시전한 시간

    #endregion

    // 외부 노출용 프로퍼티 (Get/Set 모두 허용하여 아이템 등으로 변경 가능하도록 오픈)
    public float CurrentHealth => currentHealth;
    public bool IsInvincible => isInvincible;
    public bool IsAiming => isAiming;
    public bool IsDashing => isDashing;
    public bool IsRolling => isRolling;
    public float RollDistance { get => rollDistance; set => rollDistance = value; }
    public float RollDuration { get => rollDuration; set => rollDuration = value; }
    public float RollCooldown { get => rollCooldown; set => rollCooldown = value; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentHealth = maxHealth; // 체력 기본값으로 초기화
    }

    // 무적 타이머 업데이트 로직
    private void Update()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                invincibleTimer = 0f;
                Debug.Log("[PlayerStatManager] 무적 상태가 종료되었습니다.");
            }
        }
    }

    #region [ Public API - State & Health Control ]

    // 데미지 실제 차감 로직
    /// <summary>
    /// 외부(PlayerDamageHandler)에서 데미지 차감을 요청할 때 호출됩니다.
    /// 체력이 깎이면 true를 반환합니다.
    /// </summary>
    public bool ApplyDamage(float amount)
    {
        if (amount <= 0) return false;

        // TODO: 추후 방어력 연산 추가 예정
        float finalDamage = amount; 

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            if (GameEndManager.Instance != null && !GameEndManager.Instance.IsGameEnded)
            {
                GameEndManager.Instance.TriggerGameEnd(GameEndReason.PlayerDeath);
            }
            Debug.Log("[PlayerStatManager] 플레이어 체력 0에 도달하여 게임 오버.");
        }

        return true;
    }

    /// <summary>
    /// 피격, 구르기 등 무적이 필요한 상황에 호출됩니다.
    /// 기존 무적 시간과 비교하여 더 긴 시간으로 덮어씌웁니다.
    /// </summary>
    public void SetInvincible(float time)
    {
        if (time <= 0) return;

        if (time > invincibleTimer)
        {
            invincibleTimer = time;
            isInvincible = true;
            Debug.Log($"[PlayerStatManager] 무적 시간 갱신됨. 남은 시간: {invincibleTimer:F1}초");
        }
    }

    // 조준 상태 제어
    public bool TryStartAim()
    {
        if (isRolling) return false;
        isAiming = true;
        return true;
    }

    public void StopAim()
    {
        if (isAiming) isAiming = false;
    }
    
    /// <summary>
    /// PlayerController에서 대쉬 키를 눌렀을 때 호출됩니다.
    /// 조건(스턴, 쿨타임 등)을 검사한 뒤 대쉬 상태로 진입합니다.
    /// </summary>
    public bool TryStartDash()
    {
        // TODO: 추후 스턴 상태, 대쉬 쿨타임, 스태미나 부족 등의 조건 검사 로직 추가 예정
        
        
        // 1. 구르기 중에는 대쉬 불가능
        if (isRolling) return false;
        
        isDashing = true;
        return true;
    }

    /// <summary>
    /// 대쉬 키를 떼거나, 피격/스턴 등 외부 요인에 의해 대쉬가 강제 해제될 때 호출됩니다.
    /// </summary>
    public void StopDash()
    {
        if (isDashing)
        {
            isDashing = false;
        }
    }

    /// <summary>
    /// PlayerController에서 구르기 키를 눌렀을 때 호출됩니다.
    /// 쿨타임을 검사하고, 성공 시 무적 상태를 임시로 발동합니다.
    /// </summary>
    public bool TryStartRoll()
    {
        // 1. 쿨타임 검사
        if (Time.time < lastRollTime + rollCooldown)
        {
            Debug.Log("Dodge CoolTime is not 완료!");
            return false;
        }

        // 2. 구르기 상태 돌입
        isRolling = true;
        lastRollTime = Time.time;
        Debug.Log("Dodge!");

        // 3. 무적 로직 호출 : 구르기 지속 시간만큼 무적 부여
        SetInvincible(rollDuration);

        return true;
    }

    /// <summary>
    /// 지정된 구르기 시간이 끝나면 PlayerController가 호출하여 상태를 해제합니다.
    /// </summary>
    public void EndRoll()
    {
        isRolling = false;
    }

    // [ 추가됨 ] 플레이어 체력을 최대치로 회복 (US-1.09 연계)
    public void HealToMax()
    {
        currentHealth = maxHealth;
        // TODO: 추후 UIManager를 통해 체력바 UI 업데이트 이벤트 송출 필요
    }
    

    #endregion

    #region [ Public API - Stat Getter ]
    
    /// <summary>
    /// 현재 플레이어의 최종 이동 속도를 반환합니다.
    /// 대쉬 상태일 경우 배율이 적용된 속도를 반환합니다.
    /// 추후 아이템이나 디버프에 의한 속도 계산 로직이 이곳에 추가될 수 있습니다.
    /// </summary>
    public float GetMoveSpeed()
    {
        // 최우선순위: 대쉬 - 대쉬 중이라면 기본 속도에 배율을 곱해서 반환, 아니면 기본 속도 반환
        if (isDashing) return baseMoveSpeed * dashSpeedMultiplier;
        // 2순위: 조준 - 조준 중이라면 조준시 이동속도를 반환
        if (isAiming) return aimMoveSpeed;
        // 3순위: 기본
        return baseMoveSpeed;
    }
    
    #endregion
}