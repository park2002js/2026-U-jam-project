using UnityEngine;
using UnityEngine.SceneManagement;

public class UILevelSelectManager : MonoBehaviour
{
    public enum Level
    {
        None = 0,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3
    }

    [SerializeField] private string level1SceneName;
    [SerializeField] private string level2SceneName;
    [SerializeField] private string level3SceneName;

    // 선택한 레벨의 씬으로 이동한다. None은 아무 동작도 하지 않는다.
    public void LevelSelect(Level level)
    {
        string sceneName = GetSceneName(level);
        if (sceneName == null) return;

        if (!IsLevelAvailable(level))
        {
            Debug.LogWarning($"{level}에 연결할 씬 이름을 지정하고 Build Settings에 등록해 주세요. (현재: '{sceneName}')", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // Button의 On Click에서는 enum 대신 정수(0: None, 1~3: 레벨)를 지정한다.
    public void LevelSelect(int level)
    {
        LevelSelect((Level)level);
    }

    /// <summary>
    /// 레벨에 연결된 씬이 Build Settings에 등록되어 실제로 불러올 수 있는지 확인합니다.
    /// 불가능한 레벨은 레벨 선택 화면에서 잠김으로 표시됩니다.
    /// </summary>
    public bool IsLevelAvailable(Level level)
    {
        string sceneName = GetSceneName(level);
        return !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
    }

    public bool IsLevelAvailable(int level) => IsLevelAvailable((Level)level);

    private string GetSceneName(Level level)
    {
        switch (level)
        {
            case Level.Level1: return level1SceneName ?? string.Empty;
            case Level.Level2: return level2SceneName ?? string.Empty;
            case Level.Level3: return level3SceneName ?? string.Empty;
            default: return null;
        }
    }
}
