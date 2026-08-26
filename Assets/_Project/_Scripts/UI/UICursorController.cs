using UJam.Runtime.Phase;
using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 항상 활성화된 UIManagers에 부착하여 GameManager의 현재 Phase에 맞는 마우스 커서를 유지합니다.
    /// </summary>
    public class UICursorController : MonoBehaviour
    {
        [Header("Preparation Cursor")]
        [SerializeField, Tooltip("Cursor 타입으로 임포트한 Texture2D를 연결합니다. 비어 있으면 기본 커서를 사용합니다.")]
        private Texture2D _preparationCursor;
        [SerializeField, Tooltip("텍스처 왼쪽 위 기준 클릭 위치(픽셀)입니다.")]
        private Vector2 _preparationHotspot;

        [Header("Combat Cursor")]
        [SerializeField, Tooltip("전투 중 사용할 Cursor 텍스처입니다. 비어 있으면 기본 커서를 사용합니다.")]
        private Texture2D _combatCursor;
        [SerializeField] private Vector2 _combatHotspot;

        private GameManager _gameManager;

        /// <summary>
        /// GameManager.Instance의 Phase와 게임 오버 이벤트를 구독하고 커서를 즉시 적용합니다.
        /// </summary>
        private void OnEnable()
        {
            BindGameManager();
        }

        /// <summary>
        /// GameManager.Awake가 늦게 실행된 경우에도 씬 시작 시 싱글톤 연결을 완료합니다.
        /// </summary>
        private void Start()
        {
            if (_gameManager == null) BindGameManager();
        }

        /// <summary>
        /// Inspector 참조 없이 실제 구독 대상을 저장하고 현재 상태로 커서를 복원합니다.
        /// </summary>
        private void BindGameManager()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnGameOver += RefreshCursor;
            RefreshCursor();
        }

        /// <summary>
        /// 구독을 해제하고 컨트롤러가 없어졌을 때 커스텀 커서가 남지 않도록 기본 커서로 돌립니다.
        /// </summary>
        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnPhaseChanged -= HandlePhaseChanged;
                _gameManager.OnGameOver -= RefreshCursor;
                _gameManager = null;
            }
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// GameManager의 Phase 변경 신호를 현재 커서 설정에 반영합니다.
        /// </summary>
        private void HandlePhaseChanged(PhaseState phase)
        {
            RefreshCursor();
        }

        /// <summary>
        /// 게임 창으로 돌아오면 운영체제나 에디터가 변경했을 수 있는 커서 표시를 복원합니다.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && isActiveAndEnabled) RefreshCursor();
        }

        /// <summary>
        /// 전투에는 전투 커서, 정비와 게임 오버에는 정비 커서를 적용합니다. 다음 변경까지 Unity가 유지합니다.
        /// </summary>
        private void RefreshCursor()
        {
            if (_gameManager == null) return;
            bool isCombat = !_gameManager.IsGameOver && _gameManager.CurrentPhase == PhaseState.Combat;
            Texture2D texture = isCombat ? _combatCursor : _preparationCursor;
            Vector2 hotspot = isCombat ? _combatHotspot : _preparationHotspot;
            hotspot = texture != null ? new Vector2(texture.width / 2f, texture.height / 2f) : Vector2.zero;
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
