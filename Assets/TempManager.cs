using UnityEngine;
using UnityEngine.InputSystem;
public class TempManager : MonoBehaviour
{
    [Header("카메라 모드 토글 변경 입력 설정 (New Input System)")]
    //public InputAction cameraModeToggleAction;
    [Tooltip("마우스 입력을 전달할 TPS 카메라 컨트롤러")]
    [SerializeField] private TPSCameraController tpsCamera;
    public TPSCameraController.CameraViewMode status = TPSCameraController.CameraViewMode.TPS;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
    }
    void HandleInput()
    {
        if(Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if(status == TPSCameraController.CameraViewMode.TPS)
                status = TPSCameraController.CameraViewMode.TopView;
            else
                status = TPSCameraController.CameraViewMode.TPS;

            if (tpsCamera != null)
            {
                tpsCamera.SetCameraMode(status); // 카메라 줌인/아웃 전달
            }
        }
    }
}
