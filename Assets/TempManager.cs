using UnityEngine;
using UnityEngine.InputSystem;
public class TempManager : MonoBehaviour
{
    [Header("테스트 대상 설정")]
    [Tooltip("PlayerDamageHandler가 부착된 플레이어 객체")]
    [SerializeField] private GameObject playerObject;

    [Header("발송할 데미지 정보 (Inspector 세팅)")]
    public float testDamageAmount = 100f;
    public float testInvincibleTime = 1.5f;
    public bool testBypassInvincibility = false;

    [Header("입력 키 설정 (New Input System)")]
    [Tooltip("이 키를 누르면 플레이어에게 데미지를 발송합니다.")]
    public InputAction fireDamageAction;

    // [ 추가됨 ] 페이즈 스위칭용 입력 키
    [Tooltip("이 키를 누르면 정비 <-> 전투 페이즈가 전환됩니다.")]
    public InputAction togglePhaseAction;


    private void OnEnable()
    {
        fireDamageAction.Enable();
        togglePhaseAction.Enable(); // [ 추가됨 ]
    }

    private void OnDisable()
    {
        fireDamageAction.Disable();
        togglePhaseAction.Disable(); // [ 추가됨 ]
    }

    private void Update()
    {
        if (fireDamageAction.WasPressedThisFrame())
        {
            if (playerObject != null)
            {
                // 타겟 객체에서 IDamageable 자격증을 찾음
                IDamageable damageable = playerObject.GetComponent<IDamageable>();
                
                if (damageable != null)
                {
                    // 데미지 명세서 작성
                    DamageInfo info = DamageInfo.Default(testDamageAmount, testInvincibleTime);
                    info.BypassInvincibility = testBypassInvincibility;

                    Debug.Log($"[TempManager] 데미지 발송! (데미지: {info.Amount}, 부여할 무적시간: {info.InvincibleTime}, 무적관통: {info.BypassInvincibility})");
                    
                    // 데미지 전달
                    damageable.TakeDamage(info);
                }
                else
                {
                    Debug.LogWarning("타겟 객체에 IDamageable 인터페이스를 가진 컴포넌트가 없습니다!");
                }
            }
        }

        // [ 추가됨 ] 페이즈 토글 테스트
        if (togglePhaseAction.WasPressedThisFrame())
        {
            if (PhaseManager.Instance != null)
            {
                if (PhaseManager.Instance.CurrentPhase == GamePhase.Preparation)
                {
                    PhaseManager.Instance.StartCombatPhase();
                }
                else
                {
                    PhaseManager.Instance.StartPreparationPhase();
                }
            }
        }
    }
}
