using System.Collections;
using TMPro;
using UJam.Runtime.Phase;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 체력이 모두 소진되어 게임 오버되었을 때 띄울 UI를 관리한다.
    /// 게임 흐름의 구독과 표시 시점은 UIManager가 담당하고, 이 스크립트는 결과 화면 표현과 버튼 동작만 담당합니다.
    /// '다시 하기'는 현재 스테이지 씬을 처음 들어온 것처럼 다시 불러와 첫 웨이브부터 시작하고, '타이틀로'는 타이틀 화면으로 돌아갑니다.
    /// </summary>
    public class UIGameOver : MonoBehaviour
    {
        [SerializeField] [Tooltip("게임 오버시 활성화할 UI를 할당")]
        private GameObject _gameOverUI;
        [SerializeField, Tooltip("팝업 연출을 적용할 창 본체입니다.")] private RectTransform _window;
        [SerializeField, Tooltip("도달한 웨이브를 표시합니다.")] private TMP_Text _subtitleText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;
        [SerializeField] private string _titleSceneName = SceneFlow.DefaultTitleSceneName;

        private void Awake()
        {
            if (_retryButton != null) _retryButton.onClick.AddListener(Retry);
            if (_titleButton != null) _titleButton.onClick.AddListener(GoTitle);
        }

        private void OnDestroy()
        {
            if (_retryButton != null) _retryButton.onClick.RemoveListener(Retry);
            if (_titleButton != null) _titleButton.onClick.RemoveListener(GoTitle);
        }

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

            if (_subtitleText != null)
            {
                WaveController waves = WaveController.Instance;
                _subtitleText.text = waves != null && waves.WaveCount > 0
                    ? $"웨이브 {Mathf.Max(1, waves.CurrentWaveNumber)} / {waves.WaveCount} 에서 기지가 무너졌습니다"
                    : "기지가 무너졌습니다";
            }
            if (_window != null) StartCoroutine(Pop());
            Debug.Log($"[UIGameOver] 결과 패널 활성 상태: {_gameOverUI.activeInHierarchy}", this);
        }

        /// <summary>현재 스테이지를 처음 진입한 것처럼 다시 불러와 첫 웨이브부터 시작합니다.</summary>
        public void Retry() => SceneFlow.ReloadActiveScene();

        /// <summary>타이틀 화면으로 돌아갑니다.</summary>
        public void GoTitle() => SceneFlow.LoadTitle(false, _titleSceneName);

        private IEnumerator Pop()
        {
            float t = 0f;
            const float duration = 0.4f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float x = Mathf.Clamp01(t / duration);
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float ease = 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
                _window.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, ease);
                yield return null;
            }
            _window.localScale = Vector3.one;
        }
    }
}
