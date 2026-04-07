using UnityEngine;

public abstract class DefenseBuilding : MonoBehaviour
{
    [Header("Building Settings")]
    public string buildingName;
    public int buildCost;
    public float maxHealth = 100f;
    protected float currentHealth;

    // Grid 연동을 위한 좌표 정보 (GridManager 구현 시 사용)
    protected Vector2Int gridPosition; 

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    // 건물이 배치될 때 초기화 (GridManager에서 호출 예정)
    public virtual void Setup(Vector2Int pos)
    {
        gridPosition = pos;
        Debug.Log($"{buildingName}이(가) {pos} 위치에 건설되었습니다.");
    }

    // 데미지 처리
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{buildingName} 체력: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 파괴 로직
    protected virtual void Die()
    {
        Debug.Log($"{buildingName}이(가) 파괴되었습니다.");
        // GridManager 연계: 타일 점유 해제 호출
        // GridManager.Instance.FreeTile(gridPosition); 
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Die()를 통하지 않고 에디터나 다른 방식으로 파괴될 때를 대비해 
        // 여기서도 GridManager의 점유 해제 API를 호출해주는 것이 안전합니다.
        if (GridManager.Instance != null)
        {
            GridManager.Instance.FreeTileOnDestroy(gridPosition);
        }
    }
}