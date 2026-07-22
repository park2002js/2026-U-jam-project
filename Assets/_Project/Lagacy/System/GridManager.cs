using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1f; 
    public GameObject gridVisualUI; 

    [Header("Hover Indicator")]
    public GameObject hoverIndicator; 
    
    private Renderer hoverRenderer; 
    public Color validColor = new Color(0, 1, 0, 0.5f); // 초록색 
    public Color invalidColor = new Color(1, 0, 0, 0.5f); // 빨간색 

    // 🌟 절대 오차가 없는 '정수(int)' 기반 장부로 변경!
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private void Awake()
    {
        if (hoverIndicator != null)
        {
            // 🌟 핵심 수정: (true)를 넣어야 꺼져있는 오브젝트에서도 렌더러를 찾아냅니다!
            hoverRenderer = hoverIndicator.GetComponentInChildren<Renderer>(true);
            
            if (hoverRenderer == null)
            {
                Debug.LogError("Hover Indicator에 Renderer가 없습니다! 색깔을 바꿀 수 없어요.");
            }
        }
    }

    public void ShowGrid(bool isShow)
    {
        if (gridVisualUI != null) gridVisualUI.SetActive(isShow);
    }

    public bool TryGetGridPosition(Vector3 mouseScreenPos, out Vector3 position)
    {
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
        {
            float x = Mathf.Round(hit.point.x / cellSize) * cellSize;
            float z = Mathf.Round(hit.point.z / cellSize) * cellSize;
            
            position = new Vector3(x, hit.point.y, z);
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    // 🌟 좌표(float) 대신 정확한 칸 번호(int)를 구하는 내부 함수
    private Vector2Int GetGridIndex(Vector3 position)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x / cellSize),
            Mathf.RoundToInt(position.z / cellSize)
        );
    }

    public bool IsPositionOccupied(Vector3 position)
    {
        Vector2Int gridIndex = GetGridIndex(position);
        return occupiedPositions.Contains(gridIndex);
    }

    public void MarkPositionOccupied(Vector3 position)
    {
        Vector2Int gridIndex = GetGridIndex(position);
        occupiedPositions.Add(gridIndex);
        
        // 장부에 잘 적혔는지 확인하기 위한 로그
        Debug.Log($"[장부 기록] {gridIndex.x}행, {gridIndex.y}열에 타워 설치 완료!");
    }

    public void ShowHoverIndicator(Vector3 position, bool show, bool isValid = true)
    {
        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(show);
            if (show)
            {
                Vector3 hoverPos = new Vector3(position.x, position.y + 0.1f, position.z);
                hoverIndicator.transform.position = hoverPos;
                hoverIndicator.transform.localScale = new Vector3(cellSize, cellSize, cellSize);

                if (hoverRenderer != null)
                {
                    // 🌟 최신 유니티(URP)에서도 안전하게 색상이 바뀌도록 적용
                    if (hoverRenderer.material.HasProperty("_BaseColor"))
                    {
                        hoverRenderer.material.SetColor("_BaseColor", isValid ? validColor : invalidColor);
                    }
                    else
                    {
                        hoverRenderer.material.color = isValid ? validColor : invalidColor;
                    }
                }
            }
        }
    }
}