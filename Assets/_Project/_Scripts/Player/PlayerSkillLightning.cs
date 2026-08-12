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
    public GameObject indicatorPrefab;
    public bool fitIndicatorToCell = true; // 인디케이터를 데미지 원 크기에 맞춤

    [Header("Lightning")]
    public GameObject strikeEffectPrefab;  // GroundLight (땅 빛 이펙트)
    public float effectScale = 0.3f;       // 땅 이펙트 크기 배율
    public float effectLifetime = 1.5f;    // 땅 이펙트 유지 시간(초)
    public float damage = 50f;

    [Header("Lightning Bolt (줄기)")]
    public float strikeHeight = 10f;        // 줄기 시작 하늘 높이
    public float boltWidthRatio = 0.1f;     // 줄기 굵기 = zoneRadius × 이 값
    public float boltJaggerRatio = 0.15f;   // 지그재그 폭 = zoneRadius × 이 값
    public float boltLifetime = 0.15f;      // 줄기 유지 시간
    public Material boltMaterial;           // 줄기 머티리얼
    public Color boltColor = Color.cyan;    // 줄기 색

    [Header("Damage Zone")]
    public float zoneRadius = 1f;      // 원 크기 (조준 원 + 데미지 범위). 키우면 둘 다 커짐
    public float zoneLifetime = 0.5f;  // 콜라이더 유지 시간(초). 이 시간 안에 들어온 적도 맞음

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
            indicatorObj.transform.position = center + new Vector3(0f, 0.05f, 0f);
            if (fitIndicatorToCell)
                indicatorObj.transform.localScale =
                    new Vector3(zoneRadius * 2f, indicatorObj.transform.localScale.y, zoneRadius * 2f);
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
        Vector3 center = CellToWorldCenter(row, col);

        // 1) 번개 줄기 — 코드로 LineRenderer 생성 (하늘 → 착탄점 수직)
        SpawnLightningBolt(center);

        // 2) 땅에 떨어지는 빛 이펙트(GroundLight) — 반경에 맞춰 스케일 + 수명
        if (strikeEffectPrefab != null)
        {
            GameObject fx = Instantiate(strikeEffectPrefab, center, Quaternion.identity);

            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in systems)
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            fx.transform.localScale = Vector3.one * (zoneRadius * effectScale);

            Destroy(fx, effectLifetime);
        }

        // 3) 착탄 위치에 데미지 콜라이더 생성 → 들어오는 적에게 데미지
        GameObject zoneObj = new GameObject("LightningDamageZone");
        zoneObj.transform.position = center;
        LightningDamageZone zone = zoneObj.AddComponent<LightningDamageZone>();
        zone.Setup(damage, zoneRadius, zoneLifetime, enemyMask);
    }

    // 하늘에서 착탄점으로 내리치는 번개 줄기를 코드로 생성
    private void SpawnLightningBolt(Vector3 groundPos)
    {
        GameObject boltObj = new GameObject("LightningBolt");
        LineRenderer lr = boltObj.AddComponent<LineRenderer>();

        Vector3 sky = groundPos + new Vector3(0f, strikeHeight, 0f);

        // 굵기와 지그재그 폭을 원 크기에 비례시킴
        float width = zoneRadius * boltWidthRatio;
        float jagger = zoneRadius * boltJaggerRatio;

        int segments = 8;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 point = Vector3.Lerp(sky, groundPos, t);
            if (i != 0 && i != segments)
            {
                point.x += Random.Range(-jagger, jagger);
                point.z += Random.Range(-jagger, jagger);
            }
            lr.SetPosition(i, point);
        }

        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 2;

        if (boltMaterial != null)
            lr.material = boltMaterial;
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = boltColor;
            lr.endColor = boltColor;
        }

        Destroy(boltObj, boltLifetime);
    }
    // ── 좌표 변환 (GridSystem public 값만 읽어서 내부 계산) ──

    private bool TryWorldToCell(Vector3 worldPos, out int row, out int col)
    {
        row = 0;
        col = 0;

        GridSystem grid = GridSystem.Instance;
        if (!grid.IsInitialized) return false;

        float localX = worldPos.x - grid.Origin.x;
        float localZ = worldPos.z - grid.Origin.z;

        col = Mathf.FloorToInt(localX / grid.CellWidth);
        row = Mathf.FloorToInt(localZ / grid.CellHeight);

        if (row < 0 || row >= grid.RowCount || col < 0 || col >= grid.ColumnCount)
            return false;
        return true;
    }

    private Vector3 CellToWorldCenter(int row, int col)
    {
        GridSystem grid = GridSystem.Instance;
        float x = grid.Origin.x + (col + 0.5f) * grid.CellWidth;
        float z = grid.Origin.z + (row + 0.5f) * grid.CellHeight;
        return new Vector3(x, grid.Origin.y, z);
    }
}