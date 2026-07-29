using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public class MeleeEnemy : EnemyBase
    {
        // 마지막으로 실제 피해를 전달한 시각
        private float _lastHit = float.NegativeInfinity;

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
        protected override void OnAttack(GameObject target, Vector3 attackPoint)
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

    }
}
