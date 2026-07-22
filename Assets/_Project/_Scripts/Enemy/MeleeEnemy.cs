using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class MeleeEnemy : EnemyBase
    {
        // 마지막으로 실제 피해를 전달한 시각
        private float _lastHit = float.NegativeInfinity;

        // 사망 Animation 완료에 따른 제거 중복 차단 상태
        private bool _destroyed;

        // 기존 근접 Enemy 수치를 하나의 EnemyStatus로 정의
        protected override EnemyStatus MakeStatus()
        {
            // 기존 Melee 조립 Component의 기본값을 보존한 스탯 생성
            EnemyStatus status = new EnemyStatus(
                100f,
                3.5f,
                10f,
                0.5f,
                1.5f,
                false,
                false,
                false,
                "Enemy",
                "Melee",
                "Physical");

            // 근접 Enemy 초기 스탯 반환
            return status;
        }

        // 구체 Spawn 대기 로직이 생기기 전 기존 즉시 Move 진입 유지
        protected override void OnSpawn()
        {
            // 스폰 Animation·Coroutine·외부 신호 대기가 추가되면 완료 시점에 호출하도록 변경되어야 함
            SpawnDone();
        }

        // 현재 FSM 타겟에 기존 근접 피해와 공격 대기 시간 적용
        protected override void OnAttack(Object target)
        {
            // 현재 공격 대기 시간이 지나지 않았는지 확인
            if (Time.time < _lastHit + Status.Cooldown)
            {
                // 공격 대기 중 프레임 종료
                return;
            }

            // EnemyStatus 기반 피해가 실제 타겟 계약에 전달됐는지 확인
            if (!TryDamage(target))
            {
                // 피해 전달 실패 시 공격 시각을 소비하지 않고 종료
                return;
            }

            // 실제 공격이 적용된 현재 시각 기록
            _lastHit = Time.time;
        }

        // 근접 Enemy 사망 시 타겟팅 분리용 Layer와 Animation 완료 대기 설정
        protected override void OnDead()
        {
            // 프로젝트에 존재하는 EnemyCorpse Layer 조회
            int corpseLayer = LayerMask.NameToLayer("EnemyCorpse");

            // 유효한 시체 Layer가 있을 때만 현재 GameObject에 적용
            if (corpseLayer >= 0)
            {
                gameObject.layer = corpseLayer;
            }

            // Collider 비활성화 범위와 사망 Animation 재생 Parameter가 구체화되어야 함
        }

        // FSM 상태별 Animator Controller 동작 연결
        protected override void OnAnim(EnemyStateKind state, Animator anim)
        {
            // Animator와 Controller가 연결됐는지 확인
            if (anim == null)
            {
                // Animation 연결이 없는 Enemy는 상태만 유지
                return;
            }

            // Idle·Move·Attack·Dead별 Animator Parameter와 State 이름이 구체화되어야 함
        }

        // 사망 Animation Event가 호출해 근접 Enemy를 한 번 제거
        public void DeathDone()
        {
            // 이미 제거를 요청한 중복 Animation Event 차단
            if (_destroyed)
            {
                // 중복 제거 요청 종료
                return;
            }

            _destroyed = true;

            // 현재 근접 Enemy GameObject 제거 예약
            Destroy(gameObject);
        }
    }
}
