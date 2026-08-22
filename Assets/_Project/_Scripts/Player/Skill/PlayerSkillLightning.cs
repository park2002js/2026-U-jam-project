using UnityEngine;
using UnityEngine.InputSystem;
using EnemySystem;

public class PlayerSkillLightning : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera;            // 비우면 Camera.main 사용
    public LayerMask groundMask;        // 마우스 레이캐스트로 맞출 '바닥' 레이어
    public LayerMask enemyMask;         // 데미지 판정할 '적' 레이어

    [Header("Indicator (조준 원)")]
    public GameObject indicatorPrefab;
    public bool fitIndicatorToRadius = true; // 인디케이터를 데미지 원 크기에 맞춤

    [Header("Ground Effect")]
    public GameObject strikeEffectPrefab;  // GroundLight (땅 빛 이펙트)
    public float effectScale = 0.3f;       // 땅 이펙트 크기 배율
    public float effectLifetime = 1.5f;    // 땅 이펙트 유지 시간(초)
    public float damage = 50f;

    [Header("Lightning Bolt (줄기)")]
    public GameObject lightningBoltPrefab;  // Lightning 스크립트 붙은 번개 줄기 프리팹
    public float strikeHeight = 10f;        // 줄기 시작 하늘 높이
    public float boltWidthRatio = 0.1f;     // 줄기 굵기 = zoneRadius × 이 값
    public float boltLifetime = 0.15f;      // 줄기 유지 시간

    [Header("Damage Zone")]
    public float zoneRadius = 1f;      // 원 크기 (조준 원 + 데미지 범위)
    public float zoneLifetime = 0.5f;  // 콜라이더 유지 시간(초). 이 시간 안에 들어온 적도 맞음

    [Header("Input")]
    public Key aimKey = Key.K;

    private bool isAiming;               // 현재 조준 모드인지
    private GameObject indicatorObj;     // 생성된 조준 원 인스턴스
    private Vector3 targetPos;           // 마우스가 가리키는 착탄 위치
    private bool hasValidTarget;         // 이번 프레임에 유효한 조준 지점이 있는지

    // 매 프레임 호출 — 입력 감시 + 조준 중이면 조준/발사 처리
    private void Update()
    {
        HandleAimToggle();
        if (isAiming)
        {
            UpdateIndicator();
            HandleFire();
        }
    }

    // K 키로 조준 모드를 켜고 끄는 스위치
    private void HandleAimToggle()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current[aimKey].wasPressedThisFrame)
        {
            if (isAiming) ExitAim();
            else EnterAim();
        }
    }

    // 조준 모드 진입 — 조준 원을 생성/활성화
    private void EnterAim()
    {
        isAiming = true;
        if (indicatorPrefab != null && indicatorObj == null)
            indicatorObj = Instantiate(indicatorPrefab);
        if (indicatorObj != null) indicatorObj.SetActive(true);
    }

    // 조준 모드 종료 — 조준 원을 숨기고 조준 상태 초기화
    private void ExitAim()
    {
        isAiming = false;
        hasValidTarget = false;
        if (indicatorObj != null) indicatorObj.SetActive(false);
    }

    // 마우스가 가리키는 바닥 지점을 찾아 조준 원을 그 위치로 이동 (매 프레임)
    private void UpdateIndicator()
    {
        hasValidTarget = false;
        Camera cam = aimCamera != null ? aimCamera : Camera.main;
        if (cam == null || Mouse.current == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask)) return;

        // 격자 스냅 없이 마우스가 맞춘 지점을 그대로 착탄 위치로 사용
        targetPos = hit.point;
        hasValidTarget = true;

        // 조준 원을 그 위치로 옮기고, 필요하면 데미지 반경에 맞춰 크기 조절
        if (indicatorObj != null)
        {
            indicatorObj.transform.position = targetPos + new Vector3(0f, 0.05f, 0f);
            if (fitIndicatorToRadius)
                indicatorObj.transform.localScale =
                    new Vector3(zoneRadius * 2f, indicatorObj.transform.localScale.y, zoneRadius * 2f);
        }
    }

    // 좌클릭 감지 — 유효한 조준 지점이 있으면 스킬 발동 후 조준 종료
    private void HandleFire()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!hasValidTarget) return;

        Strike(targetPos);
        ExitAim();   // 스킬을 쓰면 조준 모드가 꺼짐 (다시 K 눌러야 조준)
    }

    // 착탄 지점에 번개 줄기 + 땅 이펙트 + 데미지 콜라이더를 한 번에 생성
    private void Strike(Vector3 center)
    {
        // 1) 번개 줄기 — 프리팹을 하늘 → 착탄점으로 세워 생성
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

        // 3) 착탄 위치에 데미지 콜라이더 생성 → 판정은 LightningDamageZone에 위임
        GameObject zoneObj = new GameObject("LightningDamageZone");
        zoneObj.transform.position = center;
        LightningDamageZone zone = zoneObj.AddComponent<LightningDamageZone>();
        zone.Setup(damage, zoneRadius, zoneLifetime, enemyMask);
    }

    // 번개 줄기 프리팹을 하늘 → 착탄점으로 세워서 생성
    private void SpawnLightningBolt(Vector3 groundPos)
    {
        if (lightningBoltPrefab == null) return;

        Vector3 sky = groundPos + new Vector3(0f, strikeHeight, 0f);

        GameObject bolt = Instantiate(lightningBoltPrefab, groundPos, Quaternion.identity);
        Lightning line = bolt.GetComponent<Lightning>();
        if (line != null)
            line.Setup(sky, groundPos, zoneRadius * boltWidthRatio, boltLifetime);
    }
}