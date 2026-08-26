using System;
using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 항상 활성화된 UIManagers에 부착하여 게임 흐름 신호에 따른 UI 표시 전환을 한곳에서 처리합니다.
    /// HP, 재화 등 개별 UI의 내용 갱신은 각 UI 스크립트가 담당합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Serializable]
        private class UIVisibility
        {
            [SerializeField] private GameObject[] _show = Array.Empty<GameObject>();
            [SerializeField] private GameObject[] _hide = Array.Empty<GameObject>();

            /// <summary>
            /// UIManager가 해당 신호를 받았을 때만 표시를 변경합니다. 두 목록에 모두 있으면 숨김을 우선합니다.
            /// </summary>
            public void Apply()
            {
                foreach (GameObject target in _show)
                {
                    if (target != null) target.SetActive(true);
                }
                foreach (GameObject target in _hide)
                {
                    if (target != null) target.SetActive(false);
                }
            }
        }

        [SerializeField, Tooltip("정비 진입 시 Tablet, 전투 시작 버튼 등을 켜고 필요한 UI를 숨깁니다.")]
        private UIVisibility _preparation = new UIVisibility();
        [SerializeField, Tooltip("전투 진입 시 Tablet, 전투 시작 버튼 등을 숨깁니다.")]
        private UIVisibility _combat = new UIVisibility();
        [SerializeField, Tooltip("게임 오버 시 켜거나 끌 UI입니다. Phase 표시보다 우선합니다.")]
        private UIVisibility _gameOver = new UIVisibility();
        [SerializeField, Tooltip("결과 화면 표현이 필요하면 연결합니다. 게임 오버 패널을 Game Over의 Show에 직접 넣어도 됩니다.")]
        private UIGameOver _gameOverView;

        private GameManager _gameManager;

        /// <summary>
        /// GameManager.Instance의 흐름 이벤트를 구독하고 현재 상태로 UI를 동기화합니다.
        /// </summary>
        private void OnEnable() => BindGameManager();

        // 다른 오브젝트의 Awake 순서와 관계없이 Start까지 구독을 완료한다.
        private void Start()
        {
            if (_gameManager == null) BindGameManager();
        }

        private void BindGameManager()
        {
            if (_gameManager != null) return;
            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnGameOver += HandleGameOver;
            if (_gameManager.IsGameOver) HandleGameOver();
            else HandlePhaseChanged(_gameManager.CurrentPhase);
        }

        /// <summary>
        /// 실제로 구독했던 GameManager에서 이벤트를 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_gameManager == null) return;
            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            _gameManager.OnGameOver -= HandleGameOver;
            _gameManager = null;
        }

        /// <summary>
        /// GameManager의 Phase 알림에 해당하는 표시 목록을 한 번 적용하며 지속적으로 활성 상태를 강제하지 않습니다.
        /// </summary>
        private void HandlePhaseChanged(PhaseState phase)
        {
            if (_gameManager == null || _gameManager.IsGameOver) return;
            if (phase == PhaseState.Preparation) _preparation.Apply();
            else if (phase == PhaseState.Combat) _combat.Apply();
        }

        /// <summary>
        /// GameManager의 게임 오버 알림에 맞춰 UI를 전환하고 UIGameOver에 결과 화면 표현을 요청합니다.
        /// </summary>
        private void HandleGameOver()
        {
            Debug.Log("[UIManager] Game Over 신호 수신", this);
            _gameOver.Apply();
            if (_gameOverView != null) _gameOverView.DefeatGame();
        }

        /// <summary>
        /// 전투 시작 Button.OnClick에 연결합니다. GameManager를 통해 PhaseSystem에 전환 판단을 요청합니다.
        /// </summary>
        public void EventStartCombatPhase()
        {
            if (GameManager.Instance != null) GameManager.Instance.StartCombatPhase();
        }
    }
}
