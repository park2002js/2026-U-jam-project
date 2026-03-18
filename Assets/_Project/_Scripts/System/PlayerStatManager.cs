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

    // TODO: [Status] US-1.05 체력 시스템 연계를 위한 변수 추가 예정
    // [SerializeField] private int maxHealth = 100;
    #endregion

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

    #region [ Public API ]
    /// <summary>
    /// 현재 플레이어의 최종 이동 속도를 반환합니다.
    /// 추후 아이템이나 디버프에 의한 속도 계산 로직이 이곳에 추가될 수 있습니다.
    /// </summary>
    public float GetMoveSpeed()
    {
        return baseMoveSpeed;
    }
    #endregion
}