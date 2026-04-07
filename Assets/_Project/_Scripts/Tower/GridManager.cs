using UnityEngine;

// 이 클래스가 존재해야 DefenseBuilding에서 에러가 나지 않습니다.
public class GridManager : MonoBehaviour
{
    public static GridManager Instance; // 싱글톤

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void FreeTile(Vector2Int pos)
    {
        Debug.Log($"[GridManager] {pos} 타일 해제 로직 실행 (예정)");
    }
}