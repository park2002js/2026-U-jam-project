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

    /// <summary>
    /// 조건이 충족되었을 때(체력 0, 거점 파괴 등) 외부에서 호출하여 게임을 종료시킵니다.
    /// </summary>
    public void TriggerGameEnd(GameEndReason reason)
    {
        // 중복 실행 방지
        if (IsGameEnded) return;

        IsGameEnded = true;
        Debug.Log($"[GameEndManager] 게임 종료! 원인: {reason}");

        // 게임 종료 시 플레이어에게 전역 잠금을 겁니다.
        // 마우스 시점 허용 여부에 따라 Look 비트를 추가하거나 뺍니다.
        PlayerLockFlags flagsToLock = PlayerLockFlags.Move | PlayerLockFlags.Action | PlayerLockFlags.Damage;
        if (!allowLookOnGameEnd) flagsToLock |= PlayerLockFlags.Look;

        // this(GameEndManager)를 출처로 하여 잠금을 등록합니다.
        PlayerStatManager.Instance.AddLock(this, flagsToLock);
        

        // 종료 원인별 시퀀스 분기
        switch (reason)
        {
            case GameEndReason.PlayerDeath:
                StartCoroutine(PlayerDeathSequence());
                break;
            case GameEndReason.NexusDestroyed:
                // TODO: [Sequence] 거점 파괴 연출 (카메라 워크 등)
                // TODO: [UI] UIManager.Instance.ShowGameEndUI(reason)
                break;
            case GameEndReason.StageCleared:
                // TODO: [Sequence] 게임 클리어 연출 (승리 팡파레 등)
                // TODO: [UI] UIManager.Instance.ShowGameEndUI(reason)
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