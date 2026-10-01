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
        string sceneName;
        switch (level)
        {
            case Level.Level1:
                sceneName = level1SceneName;
                break;
            case Level.Level2:
                sceneName = level2SceneName;
                break;
            case Level.Level3:
                sceneName = level3SceneName;
                break;
            default:
                return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{level}에 연결할 씬 이름을 지정해 주세요.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    // Button의 On Click에서는 enum 대신 정수(0: None, 1~3: 레벨)를 지정한다.
    public void LevelSelect(int level)
    {
        LevelSelect((Level)level);
    }
}
