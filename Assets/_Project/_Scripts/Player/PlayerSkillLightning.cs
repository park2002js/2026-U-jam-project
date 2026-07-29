using UnityEngine;
using UnityEngine.InputSystem;
using UJam.Runtime.Grid;
using EnemySystem;

public class PlayerSkillLightning : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;            // 비우면 Camera.main 사용
    public LayerMask groundMask;        // 마우스 레이캐스트로 맞출 '바닥' 레이어
    public LayerMask enemyMask;         // 데미지 판정할 '적' 레이어

    [Header("Indicator (판대기)")]
    public GameObject indicatorPrefab;  // 마우스 따라다닐 원형 인디케이터
    public bool fitIndicatorToCell = true;

    [Header("Lightning")]
    public GameObject lightningBeamPrefab; // Lightning 컴포넌트 붙은 프리팹
    public GameObject strikeEffectPrefab;  // 착탄 이펙트 (선택)
    public float strikeHeight = 10f;       // 번개 시작 하늘 높이
    public float damage = 50f;

    [Header("Input")]
    public Key aimKey = Key.K;

    private bool isAiming;
    private GameObject indicatorObj;
    private int targetRow, targetCol;
    private bool hasValidTarget;

    private void Update()
    {
        HandleAimToggle();
        if (isAiming)
        {
            UpdateIndicator();
            HandleFire();
        }
    }

    private void HandleAimToggle()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current[aimKey].wasPressedThisFrame)
        {
            if (isAiming) ExitAim();
            else EnterAim();
        }
    }

    private void EnterAim()
    {
        isAiming = true;
        if (indicatorPrefab != null && indicatorObj == null)
            indicatorObj = Instantiate(indicatorPrefab);
        if (indicatorObj != null) indicatorObj.SetActive(true);
    }

    private void ExitAim()
    {
        isAiming = false;
        hasValidTarget = false;
        if (indicatorObj != null) indicatorObj.SetActive(false);
    }

    private void UpdateIndicator()
    {
        hasValidTarget = false;
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null || Mouse.current == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask)) return;

        if (!TryWorldToCell(hit.point, out int row, out int col)) return;

        targetRow = row;
        targetCol = col;
        hasValidTarget = true;

        Vector3 center = CellToWorldCenter(row, col);
        if (indicatorObj != null)
        {
            GridSystem grid = GridSystem.Instance;
            indicatorObj.transform.position = center + new Vector3(0f, 0.05f, 0f);
            if (fitIndicatorToCell)
                indicatorObj.transform.localScale =
                    new Vector3(grid.CellWidth, indicatorObj.transform.localScale.y, grid.CellHeight);
        }
    }

    private void HandleFire()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!hasValidTarget) return;

        StrikeCell(targetRow, targetCol);
    }

    private void StrikeCell(int row, int col)
    {
        GridSystem grid = GridSystem.Instance;
        Vector3 center = CellToWorldCenter(row, col);

        // 1) 번개 이펙트 — Lightning 재활용 (하늘 → 착탄점 수직 빔)
        if (lightningBeamPrefab != null)
        {
            GameObject beam = Instantiate(lightningBeamPrefab, center, Quaternion.identity);
            Lightning beamScript = beam.GetComponent<Lightning>();
            if (beamScript != null)
                beamScript.Setup(center + new Vector3(0f, strikeHeight, 0f), center);
        }
        if (strikeEffectPrefab != null)
            Instantiate(strikeEffectPrefab, center, Quaternion.identity);

        // 2) 그 셀에 '중심'이 있는 적만 데미지
        Vector3 boxCenter = center + Vector3.up * 1f;
        Vector3 halfExtents = new Vector3(grid.CellWidth, 2f, grid.CellHeight);
        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.identity, enemyMask);

        foreach (Collider c in hits)
        {
            if (!TryWorldToCell(c.transform.position, out int r, out int cCol)) continue;
            if (r != row || cCol != col) continue;

            Enemy enemy = c.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
    }

    // ── 좌표 변환 (GridSystem의 public 값만 읽어서 스크립트 내부에서 계산) ──

    // 월드 좌표 → 격자 (row, col). 범위 밖이면 false
    private bool TryWorldToCell(Vector3 worldPos, out int row, out int col)
    {
        row = 0;
        col = 0;

        GridSystem grid = GridSystem.Instance;
        if (!grid.IsInitialized) return false;

        // ⚠️ 가정: Col은 X축, Row는 Z축. Origin은 (0,0)셀의 모서리.
        //    격자를 다르게 깔았다면 이 두 줄만 뒤집으면 됩니다.
        float localX = worldPos.x - grid.Origin.x;
        float localZ = worldPos.z - grid.Origin.z;

        col = Mathf.FloorToInt(localX / grid.CellWidth);
        row = Mathf.FloorToInt(localZ / grid.CellHeight);

        if (row < 0 || row >= grid.RowCount || col < 0 || col >= grid.ColumnCount)
            return false;
        return true;
    }

    // 격자 (row, col) → 중앙 월드 좌표
    private Vector3 CellToWorldCenter(int row, int col)
    {
        GridSystem grid = GridSystem.Instance;
        float x = grid.Origin.x + (col + 0.5f) * grid.CellWidth;
        float z = grid.Origin.z + (row + 0.5f) * grid.CellHeight;
        return new Vector3(x, grid.Origin.y, z);
    }
}