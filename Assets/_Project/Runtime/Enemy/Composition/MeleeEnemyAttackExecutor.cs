using UJam.Runtime.Combat;
using UnityEngine;

namespace UJam.Runtime.Enemy.Composition
{
    public sealed class MeleeEnemyAttackExecutor : MonoBehaviour, IEnemyAttackExecutor, IAttackSource
    {
        // 공격 대상을 조회할 Sensor 연결 슬롯
        [SerializeField] private MeleeEnemyTargetSensor _targetSensor;
        // 기본 공격 피해량
        [SerializeField, Min(0f)] private float _rawDamage = 10f;
        // 공격 진영 식별자
        [SerializeField] private string _factionId = "Enemy";
        // 공격 식별자
        [SerializeField] private string _attackId = "Melee";
        // 피해 타입 식별자
        [SerializeField] private string _damageTypeId = "Physical";
        // 공격 사이 최소 시간
        [SerializeField, Min(0f)] private float _cooldown = 0.5f;
        // 마지막 공격 시각
        private float _lastAttackTime = float.NegativeInfinity;
        // 런타임 동작 정지 상태
        private bool _isStopped;

        // Unity 활성화 시 누락된 Sensor 참조 보완
        private void Awake()
        {
            // 같은 GameObject의 Sensor만 자동 연결
            if (_targetSensor == null) _targetSensor = GetComponentInChildren<MeleeEnemyTargetSensor>();
            // 음수 설정을 안전한 기본값으로 보정
            if (_rawDamage < 0f) _rawDamage = 0f;
            // 음수 cooldown을 안전한 기본값으로 보정
            if (_cooldown < 0f) _cooldown = 0f;
        }

        // IAttackSource가 읽는 진영 값
        public Faction Faction
        {
            get
            {
                // Inspector 진영으로 값 객체 생성
                return new Faction(_factionId);
            }
        }

        // IAttackSource가 읽는 공격 값
        public AttackId AttackId
        {
            get
            {
                // Inspector 공격 ID로 값 객체 생성
                return new AttackId(_attackId);
            }
        }

        // FSM 공격 상태에서 현재 HitZoneReceiver에 피해 적용
        public void ExecuteAttack()
        {
            // 정지 상태 공격 차단
            if (_isStopped)
            {
                // 사망 후 공격 무동작
                return;
            }
            // cooldown이 지나지 않았으면 공격 차단
            if (Time.time < _lastAttackTime + _cooldown)
            {
                // cooldown 미도달 공격 무동작
                return;
            }
            // Sensor가 없으면 공격 대상 없음 처리
            if (_targetSensor == null)
            {
                // 대상 Sensor 누락 공격 무동작
                return;
            }
            // 현재 가장 가까운 대상 조회
            if (!_targetSensor.TryGetCurrentTarget(out HitZoneReceiver receiver))
            {
                // 대상이 없으면 공격 무동작
                return;
            }
            // 양의 유효 피해량인지 확인
            if (_rawDamage <= 0f || float.IsNaN(_rawDamage) || float.IsInfinity(_rawDamage))
            {
                // 유효하지 않은 피해량 공격 무동작
                return;
            }
            // Inspector 값으로 피해 정보 생성
            DamageInfo damageInfo = new DamageInfo(this, _rawDamage, new DamageType(_damageTypeId), null, new HitContext(receiver.Zone), DamageFlags.None);
            // cooldown 시작 시각 기록
            _lastAttackTime = Time.time;
            // HitZoneReceiver에만 피해 전달
            receiver.TakeDamage(damageInfo);
        }

        // 사망 시 공격 실행을 정지
        public void StopRuntime()
        {
            // 정지 상태 기록
            _isStopped = true;
        }
    }
}
