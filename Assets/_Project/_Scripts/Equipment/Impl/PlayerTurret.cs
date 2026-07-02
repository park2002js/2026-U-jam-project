using UnityEngine;
using UnityEngine.InputSystem;
using Ballistics;

namespace Equipment.Weapon
{
    // 구체화된 돌격소총(AR) 클래스
    public class PlayerTurret : Ranged
    {
        // AR 전용 고유 로직이 필요할 경우 초기화 및 오버라이드
        public enum WeaponMode { SingleTarget, AoE }
        public WeaponMode currentMode = WeaponMode.SingleTarget;

        // UI 업데이트를 위한 이벤트 발송기
        public static System.Action<WeaponMode> OnWeaponModeChanged;

        private HitscanBehaviour hitscanLogic;
        private ProjectileBehaviour projectileLogic;

        [Header("MVP 무기 스위칭 세팅")]
        public float singleDamage = 50f;
        public float aoeDamage = 30f;
        public float aoeSpeed = 0.5f; // 범위 타격은 천천히 날아가도록 설정
        
        [Tooltip("단일 타격 시 궤적(레이저 등) 프리팹")]
        public GameObject hitscanVisualPrefab; 
        
        [Tooltip("범위 타격 시 날아갈 거대한 투사체 프리팹")]
        public GameObject aoeProjectilePrefab; 

        protected override void Start()
        {
            base.Start(); // 기존 Ranged.cs 초기화
            
            // 기존 Ballistics 시스템 재사용 
            
            hitscanLogic = new HitscanBehaviour();
            projectileLogic = new ProjectileBehaviour();

            // 시작 시 단일 타겟 모드로 세팅
            SwitchMode(WeaponMode.SingleTarget);
        }

        private void Update()
        {
            // MVP용 하드코딩: 마우스 우클릭 시 무기 스위칭
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                WeaponMode nextMode = (currentMode == WeaponMode.SingleTarget) ? WeaponMode.AoE : WeaponMode.SingleTarget;
                SwitchMode(nextMode);
            }
        }

        private void SwitchMode(WeaponMode newMode)
        {
            currentMode = newMode;

            if (currentMode == WeaponMode.SingleTarget)
            {
                // 단일 타겟: 즉발형 Hitscan 로직 장착
                ballistics = hitscanLogic;
                baseAD = singleDamage;
                PS = 0f; // 속도 불필요
                projectilePrefab = hitscanVisualPrefab;
            }
            else
            {
                // 범위 타겟: 느리게 날아가는 Projectile 로직 장착
                ballistics = projectileLogic;
                baseAD = aoeDamage;
                PS = aoeSpeed;
                projectilePrefab = aoeProjectilePrefab;
            }

            UpdateFinalStats();
            OnWeaponModeChanged?.Invoke(currentMode); // UI에 알림
            Debug.Log($"[무기 전환] 현재 모드: {currentMode}");
        }

        protected override void Shoot()
        {
            base.Shoot(); // Ranged.cs의 Shoot 로직 그대로 사용 [cite: 227]
        }
    }
}