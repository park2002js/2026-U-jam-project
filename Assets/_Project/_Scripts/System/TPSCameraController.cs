using UnityEngine;

/// <summary>
/// 플레이어를 중심으로 공전(Orbit)하는 몬스터 헌터 스타일의 TPS 카메라 컨트롤러입니다.
/// US-1.01: 마우스 입력을 받아 회전하며, 외부 스크립트에서 카메라의 거리, 감도, 각도를 제어할 수 있습니다.
/// US-1.02: 조준(Aim) 상태에 따른 카메라 줌인/줌아웃 기능을 추가했습니다.
/// US-1.10: TPS 시점과 Top View 시점 간의 모드 전환 기능을 추가했습니다. Top View 모드에서는 회전을 완전히 통제하여 맵의 정방향을 고정적으로 내려다보게 합니다.
/// </summary>
public class TPSCameraController : MonoBehaviour
{
    // 카메라 시점 모드 정의
    public enum CameraViewMode { TPS, TopView }

    #region "임시"
    [Header("Camera Offset (실시간 튜닝용)")]
    [Tooltip("평상시 카메라의 위치 (보통 플레이어 머리 위)")]
    public Vector3 normalOffset = new Vector3(0f, 1.5f, 0f);
    
    [Tooltip("조준 시 카메라의 위치 (숄더뷰 - 우측 어깨 너머)")]
    public Vector3 aimOffset = new Vector3(0.8f, 1.5f, 0f); // X값을 양수로 주면 우측으로 빠짐

    [Header("UI System")]
    [Tooltip("아까 만든 Crosshair 이미지 오브젝트를 여기에 넣으세요")]
    public GameObject crosshairUI;
    #endregion

    [Header("View Mode Settings")]
    [Tooltip("현재 카메라 시점 모드 (테스트를 위해 인스펙터에서 수정 가능)")]
    [SerializeField] private CameraViewMode currentViewMode = CameraViewMode.TPS;
    [Tooltip("Top View 시점일 때의 카메라 거리")]
    [SerializeField] private float topViewDistance = 15f;
    [Tooltip("Top View 시점일 때의 상하 고정 각도(Pitch)")]
    [SerializeField] private float topViewPitch = 75f;

    /// <summary>
    /// Top View 시점일 때 화면의 위쪽(W키 방향)이 맵의 어느 방향을 향할지 결정합니다.
    /// - 0 : 월드의 기본 Z축(북쪽)을 화면 위쪽으로 맞춥니다.
    /// - 90 : 월드의 X축(동쪽)을 화면 위쪽으로 맞춥니다. (맵이 가로로 길 경우 유용)
    /// - 180 : 월드의 -Z축(남쪽)을 화면 위쪽으로 맞춥니다.
    /// 맵의 입구나 적 스폰 지점이 화면 위쪽에 오도록 - Top View에서 자연스러워지도록 이 값을 수정하세요.
    /// </summary>
    [Tooltip("Top View 시점일 때의 화면 정방향(Yaw). 맵의 실제 북쪽/원하는 방향에 맞춰 수정하세요.")]
    [SerializeField] private float topViewYaw = 0f;


    [Header("Target Tracking")]
    [Tooltip("카메라가 바라볼 대상 (Player)")]
    [SerializeField] private Transform target;
    [Tooltip("타겟의 발밑이 아닌 상체/머리를 바라보도록 하는 오프셋")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Camera Distance")]
    [Tooltip("비조준 상태일 때의 기본 카메라 거리")]
    [SerializeField] private float normalDistance = 5f;
    [Tooltip("조준 상태일 때의 카메라 거리 (줌인)")]
    [SerializeField] private float aimDistance = 2f;
    [Tooltip("거리 전환이 일어나는 속도")]
    [SerializeField] private float zoomTransitionSpeed = 10f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 20f;

    // 현재 목표로 하는 카메라 거리
    private float targetDistance;
    // 실제 카메라에 적용될 현재 거리 (부드러운 전환을 위해 사용)
    private float currentDistance;

    [Header("Camera Sensitivity & Inversion")]
    [Tooltip("마우스 수평(X) 이동 감도")]
    [SerializeField] private float sensitivityX = 2f;
    [Tooltip("마우스 수직(Y) 이동 감도")]
    [SerializeField] private float sensitivityY = 2f;
    
    [Tooltip("수평 회전 반전 (좌측 이동 시 화면이 우측으로 도는 등)")]
    [SerializeField] private bool invertX = true;
    [Tooltip("수직 회전 반전 (상단 이동 시 화면이 하단으로 도는 등)")]
    [SerializeField] private bool invertY = true;

    [Header("Pitch Limits (Vertical Rotation)")]
    [Tooltip("카메라가 위로 올라갈 수 있는 최대 각도")]
    [SerializeField] private float maxPitch = 80f;
    [Tooltip("카메라가 아래로 내려갈 수 있는 최소 각도")]
    [SerializeField] private float minPitch = -20f;

    // 내부 계산용 변수 (현재 카메라의 회전 각도)
    private float currentYaw = 0f;
    private float currentPitch = 15f;
    private bool isAiming = false;       // 현재 조준 중인지 여부
    private Vector3 currentOffset;       // 실시간으로 변하는 현재 오프셋 (보간용)

    private void Start()
    {
        // 초기 거리 설정
        targetDistance = normalDistance;
        currentDistance = normalDistance;

        currentOffset = normalOffset;
        if (crosshairUI != null) crosshairUI.SetActive(false);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 모드에 따른 목표 거리 및 각도 설정
        float activeTargetDistance = targetDistance;

        if (currentViewMode == CameraViewMode.TopView)
        {
            // Top View 모드일 경우: 거리, 상하 각도(Pitch), 좌우 각도(Yaw)를 모두 지정된 값으로 강제 보간
            activeTargetDistance = topViewDistance;
            currentPitch = Mathf.Lerp(currentPitch, topViewPitch, zoomTransitionSpeed * Time.deltaTime);
            currentYaw = Mathf.LerpAngle(currentYaw, topViewYaw, zoomTransitionSpeed * Time.deltaTime);
        }

        // // 2. 카메라 거리의 부드러운 보간(Lerp) 처리 (동일)
        // currentDistance = Mathf.Lerp(currentDistance, activeTargetDistance, zoomTransitionSpeed * Time.deltaTime);
        // //currentDistance = Mathf.Lerp(currentDistance, **targetDistance**, zoomTransitionSpeed * Time.deltaTime);

        // // 3. 카메라의 최종 위치 및 회전 계산
        // // 구면 좌표계를 기반으로 Pitch(상하)와 Yaw(좌우)를 Quaternion으로 변환
        // Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        
        // // 타겟 위치에서 계산된 회전값과 거리만큼 뒤로 물러난 위치가 카메라의 위치
        // Vector3 focusPosition = target.position + targetOffset;

        // --- 추가 및 수정된 구간 ---
        // 2. 오프셋 목표값 결정 및 부드러운 보간
        Vector3 targetOff = isAiming ? aimOffset : normalOffset;
        currentOffset = Vector3.Lerp(currentOffset, targetOff, zoomTransitionSpeed * Time.deltaTime);

        // 2-1. 카메라 거리 보간 (기존 로직 유지)
        currentDistance = Mathf.Lerp(currentDistance, activeTargetDistance, zoomTransitionSpeed * Time.deltaTime);

        // 3. 카메라 최종 위치 계산
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // [중요 수정]: focusPosition 계산 시 캐릭터의 회전(target.rotation)을 오프셋에 곱해줍니다.
        // 이렇게 해야 캐릭터가 보는 방향을 기준으로 오른쪽/왼쪽 어깨 너머 시점이 형성됩니다.
        // ✨ 해결: 마우스(카메라)의 좌우 회전각(Yaw)만을 기준으로 오프셋 방향을 계산하여 즉각적으로 반응하게 만듦
        Quaternion yawRotation = Quaternion.Euler(0, currentYaw, 0);
        Vector3 focusPosition = target.position + (yawRotation * currentOffset);
        //Vector3 focusPosition = target.position + (target.rotation * currentOffset);
        // --------------------------

        Vector3 newPosition = focusPosition - (rotation * Vector3.forward * currentDistance);

        transform.position = newPosition;
        // transform.LookAt(focusPosition) 대신 직접 rotation을 주입합니다.
        // LookAt은 Pitch가 90도가 될 때 Up 벡터 계산을 실패하여 Yaw를 0으로 초기화해버리지만,
        // 이 방식은 이미 계산된 오일러 각도를 직접 넣기 때문에 오작동 하지 않고 카메라는 항상 타겟(오프셋 적용)을 바라봄
        transform.rotation = rotation;
    }

    #region [ Public API - 외부 제어용 ]
    
    /// <summary>
    /// 외부 매니저(예: PhaseManager)에서 카메라 모드를 변경할 때 호출합니다.
    /// </summary>
    public void SetCameraMode(CameraViewMode newMode)
    {
        currentViewMode = newMode;
        
        // 조준 중 TopView로 넘어가는 예외 상황 등을 방지하기 위해 거리 초기화
        if (newMode == CameraViewMode.TopView)
        {
            targetDistance = normalDistance; 
        }
    }

    /// <summary>
    /// PlayerController 등에서 마우스 이동량(Delta)을 전달받아 카메라 각도를 갱신합니다.
    /// </summary>
    public void RotateCamera(Vector2 mouseDelta)
    {
        // Top View일 경우, 델타 계산 자체를 건너뛰어 마우스 입력을 원천 차단함으로써 화면이 회전하려다 다시 돌아오는 반쪽짜리 떨림 현상을 제거
        if (currentViewMode == CameraViewMode.TopView) return;

        // Invert 설정에 따라 마우스 입력값의 부호를 결정하여 누적
        float yawInput = invertX ? -mouseDelta.x : mouseDelta.x;
        currentYaw += yawInput * sensitivityX;

        float pitchInput = invertY ? -mouseDelta.y : mouseDelta.y;
        currentPitch += pitchInput * sensitivityY;

        // Pitch(상하 각도)는 지정된 범위를 넘어가지 않도록 제한
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
    }

    /// <summary>
    /// 조준 상태 여부에 따라 목표 카메라 거리를 변경합니다.
    /// </summary>
    public void SetAimState(bool isAiming)
    {
        // Top View 상태에서는 조준 줌인을 무시함
        
        if (currentViewMode == CameraViewMode.TopView) return;

        this.isAiming = isAiming; // 상태 저장
        targetDistance = isAiming ? aimDistance : normalDistance;

        // 추가: 조준선 UI 켜고 끄기
        if (crosshairUI != null) crosshairUI.SetActive(isAiming);
    }

    /// <summary>
    /// 줌인/줌아웃, 씬 전환 연출 등을 위해 카메라와 타겟 사이의 거리를 동적으로 설정합니다.
    /// </summary>
    public void SetCameraDistance(float newDistance)
    {
        normalDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        if (targetDistance != aimDistance && currentViewMode == CameraViewMode.TPS) // 조준 중이 아닐 때 + TPS 시점일 때 만 타겟 거리 즉각 업데이트
        {
            targetDistance = normalDistance;
        }
    }

    /// <summary>
    /// 설정 창(UI) 등에서 마우스 감도를 동적으로 변경할 때 호출합니다.
    /// </summary>
    public void SetSensitivity(float x, float y)
    {
        sensitivityX = x;
        sensitivityY = y;
    }

    #endregion
}