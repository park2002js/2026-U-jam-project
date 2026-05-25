using UnityEngine;
using UnityEngine.UI; // 기본 UI 텍스트 사용
using EnemySystem;

public class SimpleUIManager : MonoBehaviour
{
    [Header("상태 표시 UI (Text)")]
    public Text hpText;
    public Text enemyCountText;
    public Text phaseText;

    [Header("게임 오버 UI (Panel)")]
    public GameObject gameOverPanel;
    public Text gameOverReasonText;

    private void Start()
    {
        // 시작할 때 게임오버 화면은 숨겨둡니다.
        if (gameOverPanel != null) 
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. 플레이어 체력 표시 (PlayerStatManager 활용)
        if (hpText != null && PlayerStatManager.Instance != null)
        {
            hpText.text = $"현재 체력: {PlayerStatManager.Instance.CurrentHealth}";
        }

        // 2. 남은 적의 수 표시 (코드 수정 없이 씬에 활성화된 적의 개수를 셈)
        if (enemyCountText != null)
        {
            int currentEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;
            enemyCountText.text = $"남은 적: {currentEnemies} 명";
        }

        // 3. 현재 페이즈 표시 (PhaseManager 활용)
        if (phaseText != null && PhaseManager.Instance != null)
        {
            // Preparation 또는 Combat 문자열이 출력됨
            if("Combat" == PhaseManager.Instance.CurrentPhase.ToString())
            {
                phaseText.text = "Phase : 전투 단계";
            }
            else
            {
                phaseText.text = "Phase : 정비 단계";
            }
        }

        // 4. 게임 오버 UI 팝업 (GameEndManager 활용)
        if (GameEndManager.Instance != null && GameEndManager.Instance.IsGameEnded)
        {
            if (gameOverPanel != null && !gameOverPanel.activeSelf)
            {
                gameOverPanel.SetActive(true); // 게임 오버 창 띄우기

                if (gameOverReasonText != null)
                {
                    // 체력이 0이하라면 플레이어 사망, 아니면 거점 파괴
                    if (PlayerStatManager.Instance != null && PlayerStatManager.Instance.CurrentHealth <= 0)
                        gameOverReasonText.text = "플레이어 사망!";
                    else
                        gameOverReasonText.text = "거점 파괴됨!";
                }
            }
        }
    }
}