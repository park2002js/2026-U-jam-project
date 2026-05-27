using UnityEngine;
using Ballistics;

namespace Equipment.Weapon
{
    // 구체화된 돌격소총(AR) 클래스
    public class AR : Ranged
    {
        // AR 전용 고유 로직이 필요할 경우 초기화 및 오버라이드
        protected override void Start()
        {
            base.Start();
            // AR 초기 스탯 임시 세팅

            baseAD = 10f; // 기본 공격력
            baseAS = 5f;  // 기본 공격 속도 (초당 5회 발사 = 0.2초 쿨타임)
            PS = 70f;     // 투사체 속도 (분할 레이캐스트의 움직임이 보이도록 적당히 느린 40 설정)

            // AR의 발사 방식을 Ballistics의 객체를 생성하는 것으로 결정
            ballistics = new SegmentRaycastBehaviour();

            // 설정한 수치로 최종 스탯 갱신
            UpdateFinalStats();
            
            Debug.Log($"[AR] 초기화 완료 - AD: {currentAD}, AS: {currentAS}, PS: {PS}");
        }

        protected override void Shoot()
        {
            // Ranged의 기본 Shoot(목표점 계산 등)을 수행
            base.Shoot();

            // Muzzle Flash 
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                // 총구 위치와 회전값에 맞춰 이펙트 생성
                GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                // 총구를 따라가도록 부모 설정 (움직이면서 쏠 때 자연스럽게)
                flash.transform.SetParent(firePoint);
                // 0.1초 뒤 자동 삭제
                Destroy(flash, 0.1f);
            }
            
            // TODO: 탄환 발사 로직 외에, AR에만 존재하는 고유 연출 (반동, 사운드) 추가
            Debug.Log("[AR] 사격 실행!");
        }
    }
}