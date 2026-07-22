using UJam.Runtime.Enemy.FSM;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public sealed class EnemyFSM
    {
        // 상태 행동을 구체화하는 Enemy
        private readonly EnemyBase _enemy;

        // 상태별 Animation을 재생할 Animator
        private readonly Animator _anim;

        // 현재 상태 종류
        private EnemyStateKind _state;

        // FSM 초기화 여부
        private bool _ready;

        // 최초 Spawn 완료 여부
        private bool _spawnDone;

        // 현재 타겟 사거리 포함 여부
        private bool _inRange;

        // 현재 공격과 이동에 사용할 Unity Target Object
        private Object _target;

        // Enemy와 네 상태를 한 번 연결
        public EnemyFSM(EnemyBase enemy, Animator anim)
        {
            _enemy = enemy;
            _anim = anim;

            // 재사용할 네 상태 객체 생성
            Idle = new IdleState(enemy, this);
            Move = new MoveState(enemy, this);
            Attack = new AttackState(enemy, this);
            Dead = new DeadState(enemy, this);
        }

        // 최초 Spawn과 대기를 담당하는 상태
        public IdleState Idle { get; }

        // 목적지 이동을 담당하는 상태
        public MoveState Move { get; }

        // 현재 타겟 공격을 담당하는 상태
        public AttackState Attack { get; }

        // terminal 사망을 담당하는 상태
        public DeadState Dead { get; }

        // 현재 상태
        public EnemyStateKind State
        {
            get
            {
                // 현재 상태 반환
                return _state;
            }
        }

        // 현재 공격과 이동에 사용할 Unity Target Object
        public Object Target
        {
            get
            {
                // 현재 타겟 반환
                return _target;
            }
        }

        // 현재 타겟 사거리 포함 여부
        public bool InRange
        {
            get
            {
                // 마지막 외부 사거리 결과 반환
                return _inRange;
            }
        }

        // Health와 최초 Idle 상태 초기화
        public bool Init()
        {
            // Enemy 누락과 중복 초기화 차단
            if (_enemy == null || _ready)
            {
                // 초기화 실패 반환
                return false;
            }

            // Enemy Health 초기화 확인
            if (!_enemy.InitHealth())
            {
                // 초기화 실패 반환
                return false;
            }

            _ready = true;
            _spawnDone = false;
            _inRange = false;

            // 최초 Idle 상태 전환 결과 반환
            return Switch(EnemyStateKind.Idle);
        }

        // 상태 또는 외부 시스템의 상태 변경 요청
        public bool SetState(EnemyStateKind next)
        {
            // 초기화 전 변경 차단
            if (!_ready)
            {
                // 상태 변경 실패 반환
                return false;
            }

            // 최초 실행 뒤 Idle 재진입 차단
            if (next == EnemyStateKind.Idle && (_spawnDone || _state != EnemyStateKind.Idle))
            {
                // Idle 재진입 실패 반환
                return false;
            }

            // Spawn 완료 전에는 Dead 외 상태 차단
            if (_state == EnemyStateKind.Idle && !_spawnDone && next != EnemyStateKind.Idle && next != EnemyStateKind.Dead)
            {
                // 상태 변경 실패 반환
                return false;
            }

            // 타겟 없는 Attack 진입 차단
            if (next == EnemyStateKind.Attack && _target == null)
            {
                // 상태 변경 실패 반환
                return false;
            }

            // 중앙 상태 전환 결과 반환
            return Switch(next);
        }

        // 외부 사거리 판정으로 타겟과 상태 갱신
        public void SetRange(Object target, bool inside)
        {
            _target = target;
            _inRange = target != null && inside;

            // Move 중 타겟이 사거리 안이면 Attack 전환
            if (_state == EnemyStateKind.Move && _inRange)
            {
                SetState(EnemyStateKind.Attack);
                // 같은 보고에서 반대 전이 중복 처리 차단
                return;
            }

            // Attack 중 사거리 이탈이면 Move 전환
            if (_state == EnemyStateKind.Attack && !_inRange)
            {
                SetState(EnemyStateKind.Move);
            }
        }

        // 외부 효과로 타겟을 교체하고 Move 전환
        internal void SetTarget(Object target)
        {
            _target = target;
            _inRange = false;

            // 생존 상태만 Move 전환
            if (_ready && _state != EnemyStateKind.Dead)
            {
                SetState(EnemyStateKind.Move);
            }
        }

        // 최초 Spawn 완료 뒤 Move 전환
        internal bool FinishSpawn()
        {
            // Idle 상태에서 한 번만 완료 허용
            if (!_ready || _spawnDone || _state != EnemyStateKind.Idle)
            {
                // Spawn 완료 실패 반환
                return false;
            }

            _spawnDone = true;

            // 최초 Move 전환 결과 반환
            return Switch(EnemyStateKind.Move);
        }

        // 모든 활성 상태에서 Dead 전환
        public bool Die()
        {
            // 초기화 전과 중복 사망 차단
            if (!_ready || _state == EnemyStateKind.Dead)
            {
                // 사망 전환 실패 반환
                return false;
            }

            // terminal Dead 전환 결과 반환
            return Switch(EnemyStateKind.Dead);
        }

        // 사망 시 공유 타겟 제거
        internal void ClearTarget()
        {
            _target = null;
            _inRange = false;
        }

        // 상태 종류에 맞는 고유 함수 실행
        private bool Switch(EnemyStateKind next)
        {
            // 정의된 네 상태인지 확인
            if (next != EnemyStateKind.Idle
                && next != EnemyStateKind.Move
                && next != EnemyStateKind.Attack
                && next != EnemyStateKind.Dead)
            {
                // 알 수 없는 상태 변경 실패 반환
                return false;
            }

            // Dead 상태 이탈 차단
            if (_ready && _state == EnemyStateKind.Dead && next != EnemyStateKind.Dead)
            {
                // 상태 변경 실패 반환
                return false;
            }

            // 같은 상태 중복 실행 차단
            if (_ready && _state == next && next != EnemyStateKind.Idle)
            {
                // 현재 상태 유지 성공 반환
                return true;
            }

            _state = next;

            // 상태 Animation 먼저 갱신
            _enemy.PlayAnim(next);

            // 상태별 고유 시작 함수 선택
            switch (next)
            {
                // Idle Spawn 실행
                case EnemyStateKind.Idle:
                    Idle.Spawn();
                    break;
                // 목적지 이동 실행
                case EnemyStateKind.Move:
                    Move.Go();
                    break;
                // 공격 준비 실행
                case EnemyStateKind.Attack:
                    Attack.Ready();
                    break;
                // 사망 실행
                case EnemyStateKind.Dead:
                    Dead.Die();
                    break;
                // 정의되지 않은 상태 차단
                default:
                    // 상태 변경 실패 반환
                    return false;
            }

            // 상태 변경 성공 반환
            return true;
        }
    }
}
