using System.Collections;
using UnityEngine;

// 게임 종료(오버/클리어)를 전담하는 매니저 클래스
public enum GameEndReason { PlayerDeath, NexusDestroyed, StageCleared }

public class GameEndManager : MonoBehaviour
{
    public static GameEndManager Instance { get; private set; }

    [Header("게임 종료 정책 (Inspector)")]
    [Tooltip("게임이 종료되었을 때 마우스로 시점을 둘러보는 것을 허용할지 여부")]
    [SerializeField] private bool allowLookOnGameEnd = true;
    [Tooltip("플레이어 사망 시, UI가 뜨기 전까지 대기할 시간 (데스 애니메이션 감상 시간)")]
    [SerializeField] private float deathUIWaitTime = 3.0f;

    // 현재 게임이 종료된 상태인지 확인하는 글로벌 플래그 (적 AI, 데미지 로직 등에서 참조)
    public bool IsGameEnded { get; private set; } = false;
    public bool AllowLookOnGameEnd => allowLookOnGameEnd;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public GameEndReason CurrentEndReason { get; private set; }

    /// <summary>
    /// 조건이 충족되었을 때(체력 0, 거점 파괴 등) 외부에서 호출하여 게임을 종료시킵니다.
    /// </summary>
    public void TriggerGameEnd(GameEndReason reason)
    {
        // 중복 실행 방지
        if (IsGameEnded) return;

        // ──────────────────────────────────────────────────────────────
        // 🎯 1. 스테이지 클리어 (다음 정비 단계로 전환) 분기 처리
        // ──────────────────────────────────────────────────────────────
        if (reason == GameEndReason.StageCleared)
        {
            Debug.Log("<color=lime><b>[GameEndManager]</b></color> 스테이지 클리어! 다음 정비 단계로 전환합니다.");

            if (PhaseManager.Instance != null)
            {
                // 진짜 게임오버(IsGameEnded) 플래그를 건드리지 않고 페이즈만 빽 시킵니다!
                PhaseManager.Instance.StartPreparationPhase();
            }
            else
            {
                Debug.LogError("[GameEndManager] PhaseManager Instance를 찾을 수 없습니다!");
            }

            return; // ◀ 중요: 진짜 게임이 끝난 게 아니므로 여기서 함수를 탈출합니다!
        }

        // ──────────────────────────────────────────────────────────────
        // 💀 2. 진짜 게임오버 시퀀스 (플레이어 사망 / 거점 파괴 등)
        // ──────────────────────────────────────────────────────────────
        IsGameEnded = true; // 여기서 진짜 게임을 끝냅니다.
        CurrentEndReason = reason;
        Debug.Log($"<color=red><b>[GameEndManager]</b></color> 최종 게임 오버 접수됨! 원인: {reason}");

        // 게임 종료 시 플레이어에게 전역 잠금을 겁니다.
        PlayerLockFlags flagsToLock = PlayerLockFlags.Move | PlayerLockFlags.Action | PlayerLockFlags.Damage;
        if (!allowLookOnGameEnd) flagsToLock |= PlayerLockFlags.Look;

        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.AddLock(this, flagsToLock);
        }

        // 종료 원인별 시퀀스 분기 (Cleared가 빠졌으므로 패배 조건만 남음)
        switch (reason)
        {
            case GameEndReason.PlayerDeath:
                StartCoroutine(PlayerDeathSequence());
                break;
            case GameEndReason.NexusDestroyed:
                // TODO: [Sequence] 거점 파괴 연출 (카메라 워크 등)
                break;
        }
    }

    // 플레이어 사망 시의 연출 및 딜레이 처리 코루틴
    private IEnumerator PlayerDeathSequence()
    {
        // TODO: [Enemy AI] 모든 적의 타겟팅 리스트에서 플레이어를 강제 해제 (어그로 초기화)
        // TODO: [Animation] 플레이어 'Death' 애니메이션 Trigger 호출
        
        // (참고) IsGameEnded 플래그가 true이므로, 이 시점부터 적의 공격력은 0이 되고 타워는 무적이 됨.

        Debug.Log($"[GameEndManager] 플레이어 사망 모션 재생. {deathUIWaitTime}초 후 UI 호출 대기...");
        
        yield return new WaitForSeconds(deathUIWaitTime);
        
        // TODO: [UI] UIManager.Instance.ShowGameEndUI(GameEndReason.PlayerDeath) 호출
        Debug.Log("[GameEndManager] 대기 완료. 게임 오버 UI 팝업 (예정) - 재시작 여부 확인.");
    }
}