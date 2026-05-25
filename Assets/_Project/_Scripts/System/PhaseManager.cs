using System;
using UnityEngine;

// [ 추가됨 ] 게임의 주요 페이즈(정비, 전투) 흐름을 관리하는 매니저
public enum GamePhase { None, Preparation, Combat }

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance { get; private set; }

    // Phase 전환 정책에서 플레이어의 체력을 어떻게 할 것인지 정해야 함
    // (이전 전투 페이즈가 끝났을 때 체력값 그대로 유지, 까인 체력 중 특정 %까지만 회복, 전부 다 회복 등 중에 하나를 선택하도록)
    [Header("페이즈 전환 정책")]
    [Tooltip("정비 페이즈 진입 시 플레이어의 체력을 100% 회복할지 여부 (추후에 수정할 것)")]
    [SerializeField] private bool healOnPrepPhase = true;

    // 외부에서 현재 페이즈를 확인할 수 있는 프로퍼티
    public GamePhase CurrentPhase { get; private set; } = GamePhase.None;

    [SerializeField] private EnemySpawner enemySpawner;

    // 페이즈 변경 시 외부 시스템(스포너, UI, 타워 등)에 알리기 위한 방송 채널
    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 게임 시작 시 타워 배치 및 정비를 위해 정비 페이즈로 시작
        StartPreparationPhase();
    }

    #region [ Public API - 페이즈 전환 트리거 ]
    
    /// <summary>
    /// 전투 시작 버튼 클릭, 혹은 특정 이벤트 시 호출되어 전투 페이즈로 넘어갑니다.
    /// </summary>
    public void StartCombatPhase()
    {
        if (CurrentPhase == GamePhase.Combat) return;
        ChangePhase(GamePhase.Combat);
    }

    /// <summary>
    /// 적이 모두 제거되거나, 전투가 종료되었을 때 호출되어 정비 페이즈로 넘어갑니다.
    /// </summary>
    public void StartPreparationPhase()
    {
        if (CurrentPhase == GamePhase.Preparation) return;
        ChangePhase(GamePhase.Preparation);
    }

    #endregion

    private void ChangePhase(GamePhase newPhase)
    {
        CurrentPhase = newPhase;
        Debug.Log($"[PhaseManager] 페이즈 변경됨: {CurrentPhase} Phase");

        // 구독 중인 다른 시스템들에게 페이즈 변경 사실을 방송
        OnPhaseChanged?.Invoke(CurrentPhase);

        // 페이즈별 자체 실행 로직
        switch (CurrentPhase)
        {
            case GamePhase.Preparation:
                HandlePreparationPhase();
                break;
            case GamePhase.Combat:
                HandleCombatPhase();

                break;
        }
    }

    private void HandlePreparationPhase()
    {
        // TODO: [Camera] 카메라 시점을 Top View로 변경 지시
        // TODO: [Tower] 모든 타워의 공격 중지 지시
        // TODO: [UI] 정비 페이즈 UI(상점, 타워 배치 등) 활성화 지시
        
        if (PlayerStatManager.Instance != null)
        {
            // 정비 페이즈 진입 시 체력 회복 로직 - 현재는 Max값으로 회복시키는 옵션에 대해서만 검사를 함
            if (healOnPrepPhase)
            {
                PlayerStatManager.Instance.HealToMax();
                Debug.Log("[PhaseManager] 정비 페이즈 돌입: 플레이어 체력 100% 회복 완료.");
            }

            // [ 추가됨 ] 정비 페이즈 중에는 외부 데미지를 받지 않도록 권한 잠금
            PlayerStatManager.Instance.AddLock(this, PlayerLockFlags.Damage);
            Debug.Log("[PhaseManager] 정비 페이즈 돌입: 플레이어 데미지 판정 잠금 적용.");
        }
    }

    private void HandleCombatPhase()
    {
        // TODO: [Camera] 카메라 시점을 TPS View로 변경 지시
        // TODO: [Enemy] 적 스포너에게 웨이브 시작(생성) 지시
        // TODO: [Tower] 모든 타워의 공격 활성화 지시
        // TODO: [UI] 전투 페이즈 UI(웨이브 진행도, 남은 적 수 등) 활성화 지시

        enemySpawner.ActivateAllEnemies();

        // 전투 페이즈 시작 시 데미지 판정 잠금 해제
        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.RemoveLock(this);
            Debug.Log("[PhaseManager] 전투 페이즈 돌입: 플레이어 데미지 판정 잠금 해제.");
        }
    }
}