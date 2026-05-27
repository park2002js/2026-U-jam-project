using System.Collections; // 코루틴 사용
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    #region [ Input ]
    public enum InputMode { Toggle, Hold }

    // [하이브리드 아키텍처 핵심] 의도(Intent) 버퍼 구조체
    // 하드웨어 입력 이벤트는 오직 이 버퍼의 값만 갱신하며, 로직 처리는 하지 않습니다.
    private struct InputIntent
    {
        public Vector2 moveInput;
        public Vector2 lookInput;
        
        public bool aimHeld;
        public bool aimTriggered;
        
        public bool dashHeld;
        public bool dashTriggered;
        
        public bool rollTriggered;

        public bool attackHeld;
        public bool attackTriggered;
        
        // 매 프레임 파이프라인 처리가 끝나면 단발성 트리거를 초기화합니다.
        public void ResetTriggers()
        {
            aimTriggered = false;
            dashTriggered = false;
            rollTriggered = false;
            attackTriggered = false;
        }
    }

    private PlayerInputActions inputActions;
    // 의도 버퍼 인스턴스
    private InputIntent intent;

    // 좌클릭을 바인딩하기 위한 변수
    private InputAction manualAttackAction;
    #endregion

    #region [ Animation Rigging ]
    [Header("Animation Settings")]

    [SerializeField] private Animator animator; // 애니메이터 컨트롤러 참조를 위해 필요
    [SerializeField] private UnityEngine.Animations.Rigging.RigBuilder rigBuilder;

    private UnityEngine.Animations.Rigging.Rig bodyRig;
    private UnityEngine.Animations.Rigging.Rig aimingRig;
    
    [Header("Rigging Settings")]
    [Tooltip("조준 시 Rig Weight가 0에서 1로 차오르는 속도 (숫자가 클수록 빠름)")]
    [SerializeField] private float rigTransitionSpeed = 15f; 
    #endregion

    #region [ Components & References ]
    private Rigidbody rb;
    
    [Header("카메라 시스템")]
    [Tooltip("이동 기준이 될 카메라 Transform")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("마우스 입력을 전달할 TPS 카메라 컨트롤러")]
    [SerializeField] private TPSCameraController tpsCamera;

    [Header("무기 시스템")]
    [Tooltip("현재 장착 중인 무기 (인스펙터 할당 또는 EquipWeapon으로 장착)")]
    [SerializeField] private PlayerEquipments playerEquipments;

    #endregion

    #region [ Settings & State ]
    [Header("이동 및 회전 설정")]
    [SerializeField] private float rotationSpeed = 12f;

    [Header("입력 모드 설정 (Inspector)")]
    [SerializeField] private InputMode aimMode = InputMode.Toggle;
    [SerializeField] private InputMode dashMode = InputMode.Hold;

    // 강제 취소 시, 키를 뗐다가 다시 누르기 전까지 입력을 막는 락(Lock)
    private bool aimRequiresRepress = false;
    private bool dashRequiresRepress = false;

    // 구르기 시전 시 향할 고정된 전방 벡터
    private Vector3 rollDirection;
    #endregion

    

    // Input Action System 파일을 아예 연동시켰기에 제거
    // #region [ Input Actions ]
    // [Header("입력 설정 (New Input System)")]
    // public InputAction moveAction;
    // public InputAction lookAction; // 마우스 움직임(Delta) 감지용
    // [Tooltip("우클릭 조준 토글 입력")]
    // public InputAction aimAction; // 신규: 조준 액션
    // [Tooltip("Shift 대쉬(달리기) 입력 (Hold 방식)")]
    // public InputAction dashAction; // 신규: 대쉬 액션
    // [Tooltip("Space 구르기 입력")]
    // public InputAction rollAction; // 신규: 구르기 액션
    // #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // 2. RigBuilder가 있고, 레이어가 3개 이상 있다면 3번째(인덱스 2) Rig를 캐싱
        if (rigBuilder != null && rigBuilder.layers.Count >= 3)
        {
            aimingRig = rigBuilder.layers[2].rig;
            aimingRig.weight = 0f; // 시작할 때는 0으로 초기화

            bodyRig = rigBuilder.layers[0].rig;
            bodyRig.weight = 0f;
        }
        else
        {
            Debug.LogWarning("RigBuilder를 찾을 수 없거나 Rig Layer가 3개 미만입니다!");
        }

        //animator = GetComponent<Animator>(); // Animation 적용을 위해 필요한 파라미터를 가져와서 변수에 저장

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
            
        if (tpsCamera == null && cameraTransform != null)
            tpsCamera = cameraTransform.GetComponent<TPSCameraController>();
        
        inputActions = new PlayerInputActions();
        RegisterInputCallbacks();
    }

    private void Start()
    {
        // 게임이 시작되면 마우스를 숨기고 화면 중앙에 잠가서, 캐릭터가 정상적으로 움직이게 합니다!
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // private void OnEnable() => inputActions.Enable();
    // private void OnDisable() => inputActions.Disable();
    private void OnEnable()
    {
        // 안전장치: 혹시라도 inputActions가 비어있다면 다시 채워줍니다.
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();
            RegisterInputCallbacks();
        }
        inputActions.Enable();
    }
    private void OnDisable()
        {
            if (inputActions != null)
            {
                inputActions.Disable();
            }
        }
    // 1단계: 하드웨어 이벤트를 받아 의도(Intent)만 캐싱하는 등록부
    private void RegisterInputCallbacks()
    {
        // 이동 & 회전 (연속 데이터)
        inputActions.Player.Move.performed += ctx => intent.moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => intent.moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => intent.lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => intent.lookInput = Vector2.zero;

        // 조준
        inputActions.Player.Aim.started += ctx => { intent.aimTriggered = true; intent.aimHeld = true; };
        inputActions.Player.Aim.canceled += ctx => intent.aimHeld = false;

        // 대쉬
        inputActions.Player.Dash.started += ctx => { intent.dashTriggered = true; intent.dashHeld = true; };
        inputActions.Player.Dash.canceled += ctx => intent.dashHeld = false;

        // 구르기
        inputActions.Player.Roll.started += ctx => intent.rollTriggered = true;

        // 공격
        inputActions.Player.Attack.started += ctx => { intent.attackTriggered = true; intent.attackHeld = true; };
        inputActions.Player.Attack.canceled += ctx => intent.attackHeld = false;
    }

    private void Update()
    {
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked; // 커서 다시 잠금
            }
        }
        
        if (Cursor.lockState == CursorLockMode.None || Cursor.visible == true) 
        {
            // 의도(Intent) 버퍼를 강제로 비워버립니다. (카메라 회전, 이동 전부 멈춤)
            intent.moveInput = Vector2.zero;
            intent.lookInput = Vector2.zero;
            intent.ResetTriggers();
            return; 
        }

        HandleStatePipeline(); // 2단계: 중앙 파이프라인에서 상태 처리
        HandleRotation();      // 3단계: 시각적 회전 처리

        HandleAnimation();     // 3.5단계: 애니메이션 파라미터 적용

        HandleRigWeight();      // 3.7단계 : Rig Weight 조정

        intent.ResetTriggers(); // 4단계: 처리된 단발성 트리거 초기화
    }

    private void FixedUpdate()
    {
        if (Cursor.lockState == CursorLockMode.None || Cursor.visible == true) 
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }
        HandleMovement();      // 5단계: 물리적 이동 처리
    }

    // [핵심] 2단계: 캐싱된 Intent를 바탕으로 상태 우선순위와 정책을 결정하는 파이프라인
    private void HandleStatePipeline()
    {
        // 권한 잠금(Lock) 확인 방식으로 비활성화 된 기능들을 확인하고 이를 조정
        // 0-1. 시점(Look) 잠금 확인
        if (!PlayerStatManager.Instance.HasLock(PlayerLockFlags.Look))
        {
            if (intent.lookInput.sqrMagnitude > 0.01f && tpsCamera != null)
                tpsCamera.RotateCamera(intent.lookInput);
        }

        // 0-2. 액션(Action) 잠금 확인 (조준, 대쉬, 구르기 등 모든 스킬 행동)
        if (PlayerStatManager.Instance.HasLock(PlayerLockFlags.Action))
        {
            // 액션이 잠겼다면 진행 중인 모든 상태를 즉시 해제
            if (PlayerStatManager.Instance.IsAiming) StopAim();
            if (PlayerStatManager.Instance.IsDashing) StopDash();
            return; // 이후 로직(구르기, 대쉬 트리거 등) 전면 차단
        }


        // 1. Repress Lock 해제: 물리적으로 키를 완전히 뗐다면 락을 풀어줌
        if (!intent.aimHeld) aimRequiresRepress = false;
        if (!intent.dashHeld) dashRequiresRepress = false;

        // 2. 구르기 진행 중일 때의 예외 처리 (최우선순위 상태)
        if (PlayerStatManager.Instance.IsRolling)
        {
            // 구르기 도중 키를 누르면 무시하고 Repress 락을 걸어, 구르기가 끝난 뒤 오발동 방지
            if (intent.dashTriggered) dashRequiresRepress = true;
            if (intent.aimTriggered) aimRequiresRepress = true;

            // [정책] 구르기 도중 조준(Hold) 키를 뗐다면, 시각적 조준은 유지되더라도 내부 상태는 즉시 해제해둠
            if (aimMode == InputMode.Hold && !intent.aimHeld && PlayerStatManager.Instance.IsAiming)
            {
                StopAim();
            }
            
            return; // 다른 모든 액션 검사 중지
        }

        // 3. 구르기 시작 처리 (가장 높은 권한의 액션)
        if (intent.rollTriggered)
        {
            Vector3 desiredRollDir = transform.forward; 

            // 조준 중일 때 이동 입력이 있다면 그 방향으로 구름
            if (PlayerStatManager.Instance.IsAiming && intent.moveInput.magnitude >= 0.1f)
            {
                Vector3 camPlanarForward = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.forward;
                Vector3 camPlanarRight = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.right;
                desiredRollDir = (camPlanarForward * intent.moveInput.y + camPlanarRight * intent.moveInput.x).normalized;
            }

            if (PlayerStatManager.Instance.TryStartRoll())
            {
                // [정책] 대쉬 중 구르기 시 대쉬 강제 취소 및 Repress 요구
                if (PlayerStatManager.Instance.IsDashing)
                {
                    StopDash();
                    dashRequiresRepress = true;
                }
                
                // TODO: [Weapon] 사격 중단 (충전, 연사 취소) 로직 호출 예정

                StartCoroutine(RollRoutine(desiredRollDir));
                return; // 구르기를 시작한 프레임에는 대쉬/조준 진입을 무시함
            }
        }

        // 4. 대쉬 및 조준 처리 (상호 배제)
        bool startDashIntended = false;
        bool startAimIntended = false;

        // 대쉬 의도 파악
        if (!dashRequiresRepress)
        {
            if (dashMode == InputMode.Toggle && intent.dashTriggered)
            {
                if (PlayerStatManager.Instance.IsDashing) StopDash();
                else startDashIntended = true;
            }
            else if (dashMode == InputMode.Hold)
            {
                if (intent.dashHeld && !PlayerStatManager.Instance.IsDashing) startDashIntended = true;
                else if (!intent.dashHeld && PlayerStatManager.Instance.IsDashing) StopDash();
            }
        }

        // 조준 의도 파악
        if (!aimRequiresRepress)
        {
            if (aimMode == InputMode.Toggle && intent.aimTriggered)
            {
                if (PlayerStatManager.Instance.IsAiming) StopAim();
                else startAimIntended = true;
            }
            else if (aimMode == InputMode.Hold)
            {
                if (intent.aimHeld && !PlayerStatManager.Instance.IsAiming) startAimIntended = true;
                else if (!intent.aimHeld && PlayerStatManager.Instance.IsAiming) StopAim();
            }
        }

        // 5. 실제 상태 전이 (Conflict Resolution)
        if (startDashIntended) StartDash();
        if (startAimIntended) StartAim();

        // 6. 사격 처리 (가장 후순위, 조준 상태일 때만)
        if (PlayerStatManager.Instance.IsAiming && intent.attackHeld)
        {
            // 무기가 뭔지 알 필요 없이, 장비 관리자에게 공격 신호만 위임
            if (playerEquipments != null)
            {
                playerEquipments.ExecuteAttack();
            }
        }
    }

    // animation 적용을 실시하는 API
    private void HandleAnimation()
    {
        if (animator == null) return;

        // 1. Idle -> Walk (Blend Tree용 Float)
        float currentSpeed = intent.moveInput.magnitude;
        animator.SetFloat("Speed", currentSpeed);

        // 2. Walk -> Run (Bool)
        // PlayerStatManager에서 대쉬 상태를 가져와 그대로 애니메이터에 전달합니다.
        bool isRunning = PlayerStatManager.Instance.IsDashing;
        animator.SetBool("IsRunning", isRunning);

        // 3. 조준 상태 적용 (Bool)
        // PlayerStatManager에서 조준 상태를 가져와서 애니메이터에 그대로 전달
        bool isAiming = PlayerStatManager.Instance.IsAiming;
        animator.SetBool("IsAiming", isAiming);
    }

    // Rig Weight를 조정하는 함수 - 현재로써는 무조건 3번째 칸(2번인덱스)에 Aiming 관련 Animation Rig가 있음을 알고 이렇게 하는 것
    private void HandleRigWeight()
    {
        if (aimingRig == null || bodyRig == null) return;

        // PlayerStatManager 등에서 현재 조준 상태를 가져옴
        // (만약 Manager를 안 쓴다면 intent.aimHeld 같은 변수로 대체 가능)
        bool isAiming = PlayerStatManager.Instance.IsAiming;
        
        // 목표 가중치: 조준 중이면 1.0, 아니면 0.0
        float targetWeight = isAiming ? 1.0f : 0.0f;

        // Mathf.MoveTowards를 사용하면 지정한 속도(rigTransitionSpeed)로 아주 빠르고 '선형적'으로 목표값에 도달함
        // Body Rig과 Aim Rig 둘 다 변경되도록 함
        bodyRig.weight = aimingRig.weight = Mathf.MoveTowards(aimingRig.weight, targetWeight, Time.deltaTime * rigTransitionSpeed);
    }

    #region [ Action Executors ]
    private void StartDash()
    {
        if (PlayerStatManager.Instance.TryStartDash())
        {
            // [정책] 대쉬 진입 시 조준 상태 강제 해제 및 재입력 요구
            if (PlayerStatManager.Instance.IsAiming)
            {
                StopAim();
                aimRequiresRepress = true;
            }
        }
    }

    private void StopDash()
    {
        PlayerStatManager.Instance.StopDash();
    }

    private void StartAim()
    {
        // [정책] 조준 진입 시 대쉬 상태 강제 해제 및 재입력 요구
        if (PlayerStatManager.Instance.IsDashing)
        {
            StopDash();
            dashRequiresRepress = true;
        }

        // 매니저에게 상태 전이 요청
        if (PlayerStatManager.Instance.TryStartAim())
        {
            if (tpsCamera != null) tpsCamera.SetAimState(true);
        }
    }

    private void StopAim()
    {
        // 매니저에게 상태 해제 요청
        PlayerStatManager.Instance.StopAim();
        if (tpsCamera != null) tpsCamera.SetAimState(false);
    }
    #endregion


    // private void HandleInput()
    // {
    //     // 1. WASD 이동 입력
    //     currentMoveInput = moveAction.ReadValue<Vector2>();
        
    //     // 2. 마우스 델타(움직임 변화량) 입력
    //     currentLookInput = lookAction.ReadValue<Vector2>();

    //     // 마우스가 움직였다면 TPS 카메라에 회전 명령 전달 + 메라 회전은 구르기 중에도 가능하게 허용 (시야 확보)
    //     if (currentLookInput.sqrMagnitude > 0.01f && tpsCamera != null)
    //     {
    //         tpsCamera.RotateCamera(currentLookInput);
    //     }

    //     // 구르기 중이라면 다른 모든 상태 전환(대쉬, 조준, 중복 구르기)을 막음
    //     if (PlayerStatManager.Instance.IsRolling) return;

    //     // 3-1. 구르기 입력 처리
    //     if (rollAction.WasPressedThisFrame())
    //     {
    //         //  조준 상태 여부에 따른 구르기 방향 결정 로직
    //         Vector3 desiredRollDir = transform.forward; // 기본: 현재 캐릭터가 바라보는 정면

    //         if (isAiming && currentMoveInput.magnitude >= 0.1f)
    //         {
    //             // 조준 중이고 이동 입력이 있다면, 입력된 방향(WASD)으로 구르기 방향 설정
    //             Vector3 camPlanarForward = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.forward;
    //             Vector3 camPlanarRight = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.right;
    //             desiredRollDir = (camPlanarForward * currentMoveInput.y + camPlanarRight * currentMoveInput.x).normalized;
    //         }

    //         // 매니저 허가 시 코루틴에 계산된 방향을 넘겨줌
    //         if (PlayerStatManager.Instance.TryStartRoll())
    //         {
    //             // 조건 만족 시 구르기 타이머 코루틴 시작
    //             StartCoroutine(RollRoutine(desiredRollDir));
    //         }
    //     }

    //     // 3-2. 대쉬 입력 처리 (매니저로 신호만 전달하는 Dumb 역할)
    //     if (dashAction.WasPressedThisFrame())
    //     {
    //         PlayerStatManager.Instance.TryStartDash();
    //     }
    //     else if (dashAction.WasReleasedThisFrame())
    //     {
    //         PlayerStatManager.Instance.StopDash();
    //     }

    //     // 3-3. 대쉬 중일 경우 조준 강제 해제 (외부 개입으로 매니저 상태가 풀렸을 때도 즉각 반응)
    //     if (PlayerStatManager.Instance.IsDashing && isAiming)
    //     {
    //         isAiming = false;
    //         if (tpsCamera != null) tpsCamera.SetAimState(false);
    //         // TODO: [Weapon] 총기 사격 불가 로직 연계 예정
    //     }

    //     // 3-4. 조준 상태 토글 (대쉬 중이 아닐 때만 진입 가능, 버튼이 눌린 프레임에만 작동)
    //     if (aimAction.WasPressedThisFrame())
    //     {
    //         if(!PlayerStatManager.Instance.IsDashing)
    //         {
    //             isAiming = !isAiming; // 상태 반전

    //             if (tpsCamera != null)
    //             {
    //                 tpsCamera.SetAimState(isAiming); // 카메라 줌인/아웃 전달
    //             }
    //         }

    //         // TODO: [Animation] 조준/비조준 애니메이션 상태 전환 (CrossFade 또는 Bool 파라미터)
    //         // TODO: [UI] 조준 시 화면 중앙에 크로스헤어(Crosshair) 활성화
    //     }
    // }

    private void HandleMovement()
    {
        // 이동(Move) 잠금 확인 - 외부 개입으로 인해 플레이어가 움직일 수 없는 상태인지 확인하고 이를 적용
        if (PlayerStatManager.Instance.HasLock(PlayerLockFlags.Move))
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return; 
        }

        // 구르기 중일 때의 강제 이동 처리
        if (PlayerStatManager.Instance.IsRolling)
        {
            // 속도 = 거리 / 시간 (v = s / t)
            // 지정한 구르기 길이를 지정한 구르기 시전 시간동안 빠르게 위치이동해야 하므로
            float rollSpeed = PlayerStatManager.Instance.RollDistance / PlayerStatManager.Instance.RollDuration;
            rb.linearVelocity = new Vector3(rollDirection.x * rollSpeed, rb.linearVelocity.y, rollDirection.z * rollSpeed);
            return; // 일반 이동 로직 무시
        }

        // 일반 이동 로직
        if (intent.moveInput.magnitude >= 0.1f)
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
            Vector3 moveDirection = (camPlanarForward * intent.moveInput.y + camPlanarRight * intent.moveInput.x).normalized;

            // 매니저에서 알아서 대쉬 배율이 곱해진 속도를 반환하므로 그대로 사용함
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
        // 구르기 중에는 몸을 틀 수 없음 - 즉, 구를 때의 방향을 그대로 유지하도록 강제
        if (PlayerStatManager.Instance.IsRolling) return;

        // TODO: 자연스러운 연출
        // US-1.01
        //      후에 플레이어의 회전은, 해당 방향으로 회전하는 플레이어의 애니메이션 (예를들어 뒤로 가다가 갑자기 앞으로 틀어버리는 그러한 액션)을 구현해야할 것 같다.
        //      마우스 이동이 곧 화면 회전으로 이어지는 그 부드러움이 잘 느껴지지 않는다 해당 부분을 고려해서 다른 게임들을 많이 참고해 보아야 할 듯 하다.
        // US-1.02
        //      조준 상태에서 마우스를 급격하게 움직이면 캐릭터의 정면이 이에 딸려오는 듯한 느낌을 준다. 한마디로 버벅거리는 느낌
        //      이러한 느낌을 없애도록 자연스럽게 회전하는 느낌이 들도록 로직을 좀더 손봐야 할 듯 하다.


        // // 카메라 전방 벡터는 두 상태 모두 동일, 다만 조준과 비조준의 차이는 몸이 회전하냐 안하냐의 차이
        // ----**짐벌락으로 인해 폐기**----
        // Vector3 camForward = cameraTransform.forward;
        // camForward.y = 0f;
        // camForward.Normalize();

        
        // 회전 역시 Planar 벡터를 사용하도록 일괄 수정
        Vector3 camPlanarForward = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.forward;

        // 조준 상태에 따른 회전 방식 분기
        if (PlayerStatManager.Instance.IsAiming)
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
            if (intent.moveInput.magnitude >= 0.1f)
            {
                Vector3 camPlanarRight = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0) * Vector3.right;
                Vector3 moveDirection = (camPlanarForward * intent.moveInput.y + camPlanarRight * intent.moveInput.x).normalized;

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }
    }

    // 구르기 지속 시간을 제어하는 코루틴
    private IEnumerator RollRoutine(Vector3 dir)
    {
        // 시전 순간의 캐릭터 정면을 전달받은 방향으로 고정
        rollDirection = dir;
        
        // TODO: [Animation] 구르기 애니메이션 Trigger 호출 예정
        
        // 매니저에 설정된 시간만큼 대기
        yield return new WaitForSeconds(PlayerStatManager.Instance.RollDuration);
        
        // 시간이 끝나면 매니저에게 상태 해제 요청
        PlayerStatManager.Instance.EndRoll();
    }

}