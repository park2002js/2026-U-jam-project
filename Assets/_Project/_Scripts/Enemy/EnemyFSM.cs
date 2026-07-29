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

        // 발표용 마지막 row 거리 판정 객체
        // 발표 후 A* 경로 거리 판정으로 교체 대상
        private readonly EnemyRangeChecker _rangeChecker;

        // 현재 상태 종류
        private EnemyStateKind _state;

        // FSM 초기화 여부
        private bool _ready;

        // 최초 Spawn 완료 여부
        private bool _spawnDone;

        // 현재 타겟 사거리 포함 여부
        private bool _inRange;

        // 현재 공격에 사용할 기본 거점 GameObject
        private GameObject _target;

        // 발표용 Grid가 계산한 같은 col의 실제 공격 World 지점
        private Vector3 _attackPoint;

        // Enemy와 네 상태를 한 번 연결
        public EnemyFSM(EnemyBase enemy, Animator anim)
        {
            _enemy = enemy;
            _anim = anim;
            // 발표용 임시 거리 판정 객체 생성
            _rangeChecker = new EnemyRangeChecker(enemy != null ? enemy.transform : null);

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

        // 현재 공격에 사용할 기본 거점 GameObject
        public GameObject Target
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

        // 원거리 투사체가 향할 현재 Grid 공격 World 지점
        public Vector3 AttackPoint
        {
            get
            {
                // 마지막 거리 판정에서 계산한 공격 지점 반환
                return _attackPoint;
            }
        }

        // 발표용 Grid 거리와 직선 이동과 공격 흐름 갱신
        // 발표 후 정식 Navigation과 상태 Event 흐름으로 교체 대상
        public void Tick()
        {
            // Spawn 전과 Dead 상태의 발표용 갱신 차단
            if (!_ready
                || !_spawnDone
                || _state == EnemyStateKind.Idle
                || _state == EnemyStateKind.Dead
                || _target == null)
            {
                // 갱신할 수 없는 Frame 종료
                return;
            }

            // 발표용 마지막 row 거리와 목표 Cell
            bool inRange;
            Vector2Int targetCell;
            // 같은 col의 마지막 row 공격 World 지점
            Vector3 attackPoint;

            // 임시 거리 판정 실패 시 이동과 공격 중단
            if (!_rangeChecker.TryCheckRange(
                _enemy.AttackRangeCellCount,
                out inRange,
                out targetCell,
                out attackPoint))
            {
                _enemy.StopMovement();

                // 잘못된 Grid 상태의 Frame 종료
                return;
            }

            _inRange = inRange;
            _attackPoint = attackPoint;

            // 공격 사거리 안 진입 처리
            if (_inRange)
            {
                // Move에서 Attack으로 전환
                if (_state == EnemyStateKind.Move)
                {
                    SetState(EnemyStateKind.Attack);
                }

                // Attack 상태의 Cooldown 기반 공격 요청
                if (_state == EnemyStateKind.Attack)
                {
                    Attack.Hit();
                }

                // 사거리 안 Frame 종료
                return;
            }

            // 사거리 밖 Attack을 Move로 전환
            if (_state == EnemyStateKind.Attack)
            {
                SetState(EnemyStateKind.Move);
            }

            // Move 상태에서 발표용 마지막 row 직선 이동
            if (_state == EnemyStateKind.Move)
            {
                _enemy.Move(targetCell);
            }
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

        // 발표용 기본 거점 Target 설정과 거리 판정 연결
        internal void SetTarget(GameObject target)
        {
            _target = target;
            _inRange = false;
            _attackPoint = default;
            // 임시 거리 판정 객체에도 Target 전달
            _rangeChecker.SetTarget(target);

            // Spawn 뒤 생존 상태만 Move 전환
            if (_ready && _spawnDone && _state != EnemyStateKind.Dead)
            {
                SetState(EnemyStateKind.Move);
            }
        }

        // 공격 직전 발표용 Grid 사거리 재검사
        internal bool CanAttackTarget()
        {
            // 공격 Target과 현재 상태 확인
            if (_target == null || _state != EnemyStateKind.Attack)
            {
                // 공격 불가 반환
                return false;
            }

            // 최신 발표용 Grid 사거리 결과
            bool inRange;
            // 현재 공격에서 직접 사용하지 않는 목표 Cell
            Vector2Int targetCell;
            // 최신 같은 col의 공격 World 지점
            Vector3 attackPoint;

            // 최신 거리 판정 결과 확인
            if (!_rangeChecker.TryCheckRange(
                _enemy.AttackRangeCellCount,
                out inRange,
                out targetCell,
                out attackPoint))
            {
                // 거리 판정 실패의 공격 차단
                return false;
            }

            _inRange = inRange;
            _attackPoint = attackPoint;

            // 사거리 이탈 시 Move 복귀
            if (!_inRange)
            {
                SetState(EnemyStateKind.Move);

                // 사거리 밖 공격 차단
                return false;
            }

            // 공격 가능 반환
            return true;
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
            _attackPoint = default;
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
