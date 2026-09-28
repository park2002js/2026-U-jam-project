using UnityEngine;
using UnityEngine.SceneManagement;

public class UITitleManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("전환할 인게임 씬 이름")]
    [SerializeField] private string gameSceneName = "GameScene";

    /// 
    /// 게임 시작 버튼 클릭 시 호출
    /// 
    public void OnClickGameStart()
    {
        Debug.Log("게임 시작: " + gameSceneName + " 씬 로드");

        // 지정된 씬으로 전환
        SceneManager.LoadScene(gameSceneName);
    }

    /// 
    /// 설정 버튼 클릭 시 호출 (추후 구현)
    /// 
    public void OnClickSettings()
    {
        Debug.Log("설정 창 열기 (현재 미구현)");
        // TODO: 설정 팝업 UI 활성화 로직 추가 예정
    }

    /// 
    /// 게임 종료 버튼 클릭 시 호출
    /// 
    public void OnClickGameQuit()
    {
        Debug.Log("게임 종료 클릭됨");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 재생 멈춤
#else
    Application.Quit(); // 빌드된 게임 종료
#endif
    }
}