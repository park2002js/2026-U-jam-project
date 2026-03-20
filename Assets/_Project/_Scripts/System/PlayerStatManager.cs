using UnityEngine;

/// <summary>
/// 플레이어의 모든 스탯(이동 속도, 체력 등)을 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class PlayerStatManager : MonoBehaviour
{
    public static PlayerStatManager Instance { get; private set; }

    #region [ Core Stats ]
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
    public bool IsAiming => isAiming;
    public bool IsDashing => isDashing;
    public bool IsRolling => isRolling;
    public float RollDistance { get => rollDistance; set => rollDistance = value; }
    public float RollDuration { get => rollDuration; set => rollDuration = value; }
    public float RollCooldown { get => rollCooldown; set => rollCooldown = value; }

    // TODO: [Status] US-1.05 체력 시스템 연계를 위한 변수 추가 예정
    // [SerializeField] private int maxHealth = 100;

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
    }

    #region [ Public API - State Control ]

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

        // 3. 무적 로직 호출 (현재는 더미, US-1.06에서 완성)
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

    /// <summary>
    /// 피격, 구르기 등 무적이 필요한 상황에 호출됩니다. (US-1.06에서 세부 구현 예정)
    /// </summary>
    public void SetInvincible(float time)
    {
        // TODO: [Status] US-1.06 무적 타이머 및 상태(isInvincible) 갱신 로직 추가 예정
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