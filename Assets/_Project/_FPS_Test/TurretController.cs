using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("회전 설정 (Rotation Settings)")]
    public float mouseSensitivity = 2f;
    [Tooltip("값이 높을수록 즉각적으로 반응하고, 낮을수록 터렛이 묵직하게 따라옵니다.")]
    public float smoothTime = 15f; 
    public Vector2 pitchMinMax = new Vector2(-30f, 60f); // 상하 고개 숙임/젖힘 각도 제한

    [Header("무기 시스템 연결")]
    [Tooltip("AR 스크립트를 물고 있는 PlayerEquipments 할당")]
    public PlayerEquipments equipments;

    private float yaw;   // 좌우 회전값
    private float pitch; // 상하 회전값
    private Camera turretCamera;

    void Start()
    {
        // MVP용: 마우스 숨기고 화면 중앙에 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 시작 시 현재 각도 캐싱
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void Update()
    {
        HandleRotation();
        HandleShooting();
    }

    void HandleRotation()
    {
        if (Mouse.current == null) return;

        // New Input System의 마우스 이동량(Delta) 받아오기
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // 델타 값에 감도 적용
        yaw += mouseDelta.x * mouseSensitivity * 0.01f;
        pitch -= mouseDelta.y * mouseSensitivity * 0.01f;

        // 화면이 뒤집히지 않도록 상하 각도 제한
        pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);

        // 부드러운 회전 보간 (Slerp)
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothTime * Time.deltaTime);
    }

    void HandleShooting()
    {
        // 마우스 좌클릭 시 공격 (누르고 있으면 연사 가능하도록 isPressed 사용)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (equipments != null)
            {
                // Weapon.cs 내부에서 자체적으로 쿨타임(AS)을 계산하므로 매 프레임 호출해도 안전합니다.
                equipments.ExecuteAttack();
            }
        }
    }
}