using UnityEngine;
using UnityEngine.SceneManagement;

namespace UJam.Runtime
{
    /// <summary>
    /// 씬 사이에서 전달해야 하는 화면 흐름 요청을 보관합니다.
    /// 예) 결산 창에서 '레벨 선택으로'를 누르면 타이틀 씬이 열리자마자 레벨 선택 화면을 표시합니다.
    /// </summary>
    public static class SceneFlow
    {
        /// <summary>Build Settings에 등록된 타이틀(레벨 선택 포함) 씬 이름입니다.</summary>
        public const string DefaultTitleSceneName = "_TitleScene";

        /// <summary>타이틀 씬이 시작될 때 레벨 선택 화면을 바로 열지 여부입니다. 한 번 읽으면 초기화됩니다.</summary>
        public static bool OpenLevelSelectOnTitle { get; set; }

        /// <summary>타이틀 씬으로 이동합니다. openLevelSelect가 true면 레벨 선택 화면이 열린 상태로 시작합니다.</summary>
        public static void LoadTitle(bool openLevelSelect, string titleSceneName = DefaultTitleSceneName)
        {
            OpenLevelSelectOnTitle = openLevelSelect;
            Time.timeScale = 1f;
            SceneManager.LoadScene(string.IsNullOrWhiteSpace(titleSceneName) ? DefaultTitleSceneName : titleSceneName);
        }

        /// <summary>현재 레벨 씬을 처음부터 다시 시작합니다.</summary>
        public static void ReloadActiveScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>레벨 선택 요청을 읽고 초기화합니다.</summary>
        public static bool ConsumeOpenLevelSelect()
        {
            bool value = OpenLevelSelectOnTitle;
            OpenLevelSelectOnTitle = false;
            return value;
        }
    }
}
