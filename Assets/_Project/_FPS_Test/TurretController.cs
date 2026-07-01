using UnityEngine;

public class TurretController : MonoBehaviour
{
    [Header("회전 설정 (Rotation Settings)")]
    public float mouseSensitivity = 2f;
    [Tooltip("값이 높을수록 즉각적으로 반응하고, 낮을수록 터렛이 묵직하게 따라옵니다.")]
    public float smoothTime = 15f; 
    public Vector2 pitchMinMax = new Vector2(-30f, 60f); // 상하 고개 숙임/젖힘 각도 제한

    [Header("사격 설정 (Shooting Settings)")]
    public float weaponRange = 100f;
    public LayerMask targetLayer; // 인스펙터에서 적(Enemy) 레이어만 선택하여 피격 판정

    private float yaw;   // 좌우 회전값
    private float pitch; // 상하 회전값
    private Camera turretCamera;

    void Start()
    {
        // 마우스 커서를 화면 중앙에 고정하고 숨김 처리 (조작 테스트 필수)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        turretCamera = GetComponentInChildren<Camera>();
        if (turretCamera == null)
        {
            turretCamera = Camera.main;
        }
    }

    void Update()
    {
        HandleRotation();
        HandleShooting();
    }

    void HandleRotation()
    {
        // 마우스 입력값 받아오기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;

        // 화면이 뒤집히지 않도록 상하 각도 제한
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

        // 보간(Slerp)을 이용해 묵직한 터렛 회전 느낌 구현
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothTime * Time.deltaTime);
    }

    void HandleShooting()
    {
        // 마우스 좌클릭 시 사격
        if (Input.GetButtonDown("Fire1")) 
        {
            Fire();
        }
    }

    void Fire()
    {
        // 화면 정중앙 좌표 계산
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Ray ray = turretCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        // targetLayer에 해당하는 오브젝트가 사거리 내에 맞았을 경우
        if (Physics.Raycast(ray, out hit, weaponRange, targetLayer))
        {
            // 맞은 오브젝트에서 Enemy 컴포넌트 가져오기
            EnemyTest enemy = hit.transform.GetComponent<EnemyTest>();
            
            if (enemy != null)
            {
                // 컴포넌트가 존재한다면 피격 함수 호출
                enemy.TakeHit();
            }
            
            // 시각적 피드백: 맞은 곳에 빨간 선 그리기 (에디터 Scene 창에서 확인 가능)
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

            // 향후 연결할 데미지 처리 스크립트 예시
            // Enemy enemy = hit.transform.GetComponent<Enemy>();
            // if (enemy != null) enemy.TakeDamage(10);
        }
        else
        {
            // 허공에 쐈을 때의 궤적 표시
            Debug.DrawRay(ray.origin, ray.direction * weaponRange, Color.green, 1f);
        }
    }
}