using UnityEngine;
using UJam.Runtime.Grid;

public class GridInitialize : MonoBehaviour
{
    [Header("셀 하나 크기 (월드 단위)")]
    public float cellWidth = 1f;   // X축 방향 한 칸 폭
    public float cellHeight = 1f;  // Z축 방향 한 칸 폭

    [Header("격자 개수")]
    public int rowCount = 10;      // Z축 방향 칸 수
    public int columnCount = 10;   // X축 방향 칸 수

    [Header("원점")]
    [Tooltip("이 오브젝트 위치를 (0,0)셀의 '모서리'로 사용")]
    public bool useThisTransformAsOrigin = true;
    public Vector3 manualOrigin;

    private void Awake()
    {
        Vector3 origin = useThisTransformAsOrigin ? transform.position : manualOrigin;
        bool ok = GridSystem.Instance.Initialize(cellWidth, cellHeight, rowCount, columnCount, origin);
        if (!ok)
            Debug.LogError("[GridInitializer] 초기화 실패 — 크기/개수/원점 값을 확인하세요.");
        else
            Debug.Log($"[GridInitializer] 격자 준비 완료: {rowCount}x{columnCount}, 셀 {cellWidth}x{cellHeight}, 원점 {origin}");
    }

    // 씬 뷰에서 격자를 미리 그려서 실제 바닥과 맞추기 쉽게
    private void OnDrawGizmos()
    {
        Vector3 origin = useThisTransformAsOrigin ? transform.position : manualOrigin;
        Gizmos.color = Color.cyan;

        float w = columnCount * cellWidth;
        float h = rowCount * cellHeight;

        for (int c = 0; c <= columnCount; c++)
        {
            Vector3 a = origin + new Vector3(c * cellWidth, 0, 0);
            Gizmos.DrawLine(a, a + new Vector3(0, 0, h));
        }
        for (int r = 0; r <= rowCount; r++)
        {
            Vector3 a = origin + new Vector3(0, 0, r * cellHeight);
            Gizmos.DrawLine(a, a + new Vector3(w, 0, 0));
        }
    }
}