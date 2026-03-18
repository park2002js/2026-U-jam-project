using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    #region [ Components & References ]
    private Rigidbody rb;
    
    [Header("카메라 시스템")]
    [Tooltip("이동 기준이 될 카메라 Transform")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("마우스 입력을 전달할 TPS 카메라 컨트롤러")]
    [SerializeField] private TPSCameraController tpsCamera;
    #endregion

    #region [ Input Actions ]
    // TODO: Input Action System 파일을 아예 연동시키는 방안으로 생각

    [Header("입력 설정 (New Input System)")]
    public InputAction moveAction;
    public InputAction lookAction; // 마우스 움직임(Delta) 감지용

    [Tooltip("우클릭 조준 토글 입력")]
    public InputAction aimAction; // 신규: 조준 액션
    #endregion

    #region [ Settings & State ]
    [Header("이동 및 회전 설정")]
    [SerializeField] private float rotationSpeed = 12f;
    
    // 현재 플레이어의 상태 플래그 (추후 US-1.02 연계)
    public bool isAiming { get; private set; } = false;
    
    private Vector2 currentMoveInput;
    private Vector2 currentLookInput;
    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
            
        if (tpsCamera == null && cameraTransform != null)
            tpsCamera = cameraTransform.GetComponent<TPSCameraController>();
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        aimAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        aimAction.Disable();
    }

    private void Update()
    {
        HandleInput();
        HandleRotation(); // 시각적 회전은 Update
    }

    private void FixedUpdate()
    {
        HandleMovement(); // 물리적 이동은 FixedUpdate
    }

    private void HandleInput()
    {
        // 1. WASD 이동 입력
        currentMoveInput = moveAction.ReadValue<Vector2>();
        
        // 2. 마우스 델타(움직임 변화량) 입력
        currentLookInput = lookAction.ReadValue<Vector2>();

        // 마우스가 움직였다면 TPS 카메라에 회전 명령 전달
        if (currentLookInput.sqrMagnitude > 0.01f && tpsCamera != null)
        {
            tpsCamera.RotateCamera(currentLookInput);
        }

        // 3. 조준 상태 토글 (버튼이 눌린 프레임에만 작동)
        if (aimAction.WasPressedThisFrame())
        {
            isAiming = !isAiming; // 상태 반전

            if (tpsCamera != null)
            {
                tpsCamera.SetAimState(isAiming); // 카메라 줌인/아웃 전달
            }

            // TODO: [Animation] 조준/비조준 애니메이션 상태 전환 (CrossFade 또는 Bool 파라미터)
            // TODO: [UI] 조준 시 화면 중앙에 크로스헤어(Crosshair) 활성화
        }

        // TODO: [Input] 마우스 우클릭 입력 시 isAiming 상태 토글 로직 추가 예정 (US-1.02)
    }

    private void HandleMovement()
    {
        if (currentMoveInput.magnitude >= 0.1f)
        {
            // ----**짐벌락으로 인해 폐기**----
            // // 카메라 평면(Y축 0) 기준 전방/우측 벡터 도출
            // Vector3 camForward = cameraTransform.forward;
            // Vector3 camRight = cameraTransform.right;
            // camForward.y = 0f;
            // camRight.y = 0f;
            // camForward.Normalize();
            // camRight.Normalize();

            // 수정됨: 카메라의 물리적 forward를 가져와 y를 0으로 만드는 대신,
            // 카메라의 '좌우 회전각(Yaw)'만을 가져와 가상의 평면 정면/우측 벡터를 생성합니다.
            // 이렇게 하면 카메라가 수직으로 바닥을 보든 하늘을 보든 완벽하게 전후좌우 이동이 가능합니다.
            Vector3 camPlanarForward = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.forward;
            Vector3 camPlanarRight = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.right;

            // 카메라가 바라보는 방향을 기준으로 한 플레이어의 실제 이동 방향
            Vector3 moveDirection = (camPlanarForward * currentMoveInput.y + camPlanarRight * currentMoveInput.x).normalized;

            float moveSpeed = PlayerStatManager.Instance.GetMoveSpeed();
            // TODO: [Stat] 조준 시 이동 속도 페널티를 줄지 여부를 PlayerStatManager와 연계하여 결정
            // if(isAiming) moveSpeed *= 0.5f;
            rb.linearVelocity = new Vector3(moveDirection.x * moveSpeed, rb.linearVelocity.y, moveDirection.z * moveSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f); // 관성 미끄러짐 방지
        }
    }

    private void HandleRotation()
    {
        // TODO: 자연스러운 연출
        // US-1.01
        //      후에 플레이어의 회전은, 해당 방향으로 회전하는 플레이어의 애니메이션 (예를들어 뒤로 가다가 갑자기 앞으로 틀어버리는 그러한 액션)을 구현해야할 것 같다.
        //      마우스 이동이 곧 화면 회전으로 이어지는 그 부드러움이 잘 느껴지지 않는다 해당 부분을 고려해서 다른 게임들을 많이 참고해 보아야 할 듯 하다.
        // US-1.02
        //      조준 상태에서 마우스를 급격하게 움직이면 캐릭터의 정면이 이에 딸려오는 듯한 느낌을 준다. 한마디로 버벅거리는 느낌
        //      이러한 느낌을 없애도록 자연스럽게 회전하는 느낌이 들도록 로직을 좀더 손봐야 할 듯 하다.


        // // 카메라 전방 벡터는 두 상태 모두 동일, 다만 조준과 비조준의 차이는 몸이 회전하냐 안하냐의 차이
        // Vector3 camForward = cameraTransform.forward;
        // camForward.y = 0f;
        // camForward.Normalize();

        // ----**짐벌락으로 인해 폐기**----
        // 회전 역시 Planar 벡터를 사용하도록 일괄 수정
        Vector3 camPlanarForward = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.forward;

        // 조준 상태에 따른 회전 방식 분기
        if (isAiming)
        {
            // 조준 상태: 이동 방향과 무관하게 항상 카메라 전방을 바라봄
            if (camPlanarForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(camPlanarForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 비조준 상태: 이동하는 방향으로 캐릭터가 몸을 틂
            if (currentMoveInput.magnitude >= 0.1f)
            {
                Vector3 camPlanarRight = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.right;
                Vector3 moveDirection = (camPlanarForward * currentMoveInput.y + camPlanarRight * currentMoveInput.x).normalized;

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
}