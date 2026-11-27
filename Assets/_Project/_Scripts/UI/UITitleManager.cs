using UnityEngine;
using UnityEngine.SceneManagement;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// 타이틀 화면과 레벨 선택 화면을 하나의 씬에서 전환합니다.
    /// '게임 시작'을 누르면 타이틀 버튼이 사라지고 레벨 선택 창이 아래에서 올라옵니다.
    /// 결산 창에서 '레벨 선택으로'를 누르고 돌아온 경우에는 레벨 선택 창이 열린 상태로 시작합니다.
    /// </summary>
    public class UITitleManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField, Tooltip("타이틀 버튼 묶음(게임 시작/설정/종료)입니다.")]
        private GameObject _titleMenu;
        [SerializeField, Tooltip("레벨 선택 창입니다. UISlideIn이 있으면 열릴 때 올라오는 연출을 재생합니다.")]
        private GameObject _levelSelectPanel;
        [SerializeField, Tooltip("설정 버튼으로 여는 설정창(배경음·효과음 슬라이더)입니다.")]
        private UISettingsPanel _settingsPanel;

        [Header("Fallback")]
        [Tooltip("레벨 선택 창이 연결되지 않았을 때만 사용할 씬 이름입니다(이전 방식).")]
        [SerializeField] private string gameSceneName = "GameScene";

        /// <summary>현재 레벨 선택 창이 열려 있는지 여부입니다.</summary>
        public bool IsLevelSelectOpen => _levelSelectPanel != null && _levelSelectPanel.activeSelf;

        private void Start()
        {
            Time.timeScale = 1f;
            bool openLevelSelect = SceneFlow.ConsumeOpenLevelSelect();
            if (openLevelSelect && _levelSelectPanel != null) ShowLevelSelect();
            else ShowTitle();
        }

        /// <summary>ESC로 레벨 선택 창에서 타이틀로 돌아갑니다.</summary>
        private void Update()
        {
            if (IsLevelSelectOpen && !UISettingsPanel.AnyOpen && Input.GetKeyDown(KeyCode.Escape)) ShowTitle();
        }

        /// <summary>
        /// 게임 시작 버튼 클릭 시 호출합니다. 같은 씬의 레벨 선택 창을 엽니다.
        /// </summary>
        public void OnClickGameStart()
        {
            if (_levelSelectPanel == null)
            {
                Debug.Log("게임 시작: " + gameSceneName + " 씬 로드");
                SceneManager.LoadScene(gameSceneName);
                return;
            }
            ShowLevelSelect();
        }

        /// <summary>레벨 선택 창의 뒤로 가기 버튼에 연결합니다.</summary>
        public void OnClickBack() => ShowTitle();

        /// <summary>
        /// 설정 버튼 클릭 시 호출 (추후 구현)
        /// </summary>
        public void OnClickSettings()
        {
            if (_settingsPanel != null) _settingsPanel.Open();
            else Debug.LogWarning("[UITitleManager] 설정창(UISettingsPanel)이 연결되지 않았습니다.", this);
        }

        /// <summary>
        /// 게임 종료 버튼 클릭 시 호출
        /// </summary>
        public void OnClickGameQuit()
        {
            Debug.Log("게임 종료 클릭됨");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터 재생 멈춤
#else
            Application.Quit(); // 빌드된 게임 종료
#endif
        }

        private void ShowLevelSelect()
        {
            if (_titleMenu != null) _titleMenu.SetActive(false);
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(true);
        }

        private void ShowTitle()
        {
            if (_levelSelectPanel != null) _levelSelectPanel.SetActive(false);
            if (_titleMenu != null) _titleMenu.SetActive(true);
        }
    }
}
