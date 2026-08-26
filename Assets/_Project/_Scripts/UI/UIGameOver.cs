using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 체력이 모두 소진되어 게임 오버되었을 때 띄울 UI를 관리한다.
    /// 게임 흐름의 구독과 표시 시점은 UIManager가 담당하고, 이 스크립트는 결과 화면 표현만 담당합니다.
    /// </summary>
    public class UIGameOver : MonoBehaviour
    {
        [SerializeField] [Tooltip("게임 오버시 활성화할 UI를 할당")]
        private GameObject _gameOverUI;

        /// <summary>
        /// UIManager가 게임 오버 신호를 받은 뒤 호출하여 결과 화면을 표시합니다.
        /// </summary>
        public void DefeatGame()
        {
            if (_gameOverUI == null)
            {
                Debug.LogError("[UIGameOver] Game Over UI가 할당되지 않았습니다.", this);
                return;
            }

            // 자식 패널만 켜면 비활성화된 GameOver_UI 루트 때문에 화면에 표시되지 않는다.
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _gameOverUI.SetActive(true);
            Debug.Log($"[UIGameOver] 결과 패널 활성 상태: {_gameOverUI.activeInHierarchy}", this);
        }
    }
}
