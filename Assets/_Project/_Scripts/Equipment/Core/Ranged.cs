using UnityEngine;
using Ballistics;

namespace Equipment.Weapon
{
    // 원거리 무기 공통 클래스
    public abstract class Ranged : Weapon
    {
        [Header("Ranged Settings")]
        [Tooltip("발사될 투사체 프리팹 (껍데기용)")]
        public GameObject projectilePrefab;

        [Tooltip("총구 이펙트 (Muzzle Flash)")]
        public GameObject muzzleFlashPrefab;
        
        [Tooltip("투사체의 날아가는 속도")]
        public float PS;
        
        [Tooltip("투사체가 생성될 총구의 위치")]
        public Transform firePoint;

        protected Camera mainCamera;

        protected IBallisticsBehaviour ballistics; // 발사 로직 인터페이스 참조

        protected override void Start()
        {
            base.Start();
            mainCamera = Camera.main;
        }

        // Weapon의 PerformAttack을 오버라이드하여 사격 로직으로 연결
        protected override void PerformAttack()
        {
            Shoot();
        }

        // 원거리 무기의 실제 사격 처리 및 방향 계산 로직 (뼈대)
        protected virtual void Shoot()
        {
            if (mainCamera == null || firePoint == null) return;

            // 1. 카메라 중앙에서 화면 앞으로 조준 ray 발사
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint;

            // 2. 아무도 안맞으면 먼 허공을, 맞으면 그 지점을 목표점으로 설정
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(1000f);
            }

            // 3. 실제 총구(firePoint) 위치에서 목표점을 향하는 최종 방향 벡터 계산
            Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

            // 추가: 목표점이 총구보다 뒤에 있어서 총알이 뒤로 날아가는 버그 원천 차단
            if (Vector3.Dot(shootDirection, mainCamera.transform.forward) < 0)
            {
                shootDirection = mainCamera.transform.forward; 
            }

            // TODO: 추후 구현될 3가지 탄환 발사 로직 중 하나에 shootDirection과 PS, currentAD를 전달하여 실행할 예정
            ballistics.Execute(firePoint, shootDirection, currentAD, PS, projectilePrefab);

            Debug.Log($"[Ranged] 방향 산출 완료. 총구 위치: {firePoint.position}, 방향 벡터: {shootDirection}");
        }
    }
}