using UJam.Runtime.Enemy.FSM;
using UJam.Runtime.Enemy.Movement;
using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    public enum EnemyStateType
    {
        None,
        Idle,
        Move,
        Attack,
        Dead
    }
    public class EnemyFSM
    {
        // EnemyFSM을 보유하는 enemy 객체
        private EnemyBase _enemy;

        // 현재 상태를 구분하는 변수
        public EnemyStateType state { get; set; }

        // Stack 방식으로 관리되는 공격 대상
        private List<GameObject> targets = new List<GameObject>();
        public List<GameObject> Targets { get { return targets; } }

        // 현재 target으로 삼고 있는 대상이 유효한지 확인하는 객체
        private TargetValidator targetValidator;
        public TargetValidator TGV { get { return targetValidator; } }


        // Idle : 초기화 및 대기를 담당하는 상태
        public IdleState Idle { get; }

        // Move : 목적지 이동을 담당하는 상태
        public MoveState Move { get; }

        // Attack : 현재 타겟 공격을 담당하는 상태
        public AttackState Attack { get; }

        // Dead : 사망을 담당하는 상태
        public DeadState Dead { get; }

        // Enemy와 네 상태를 한 번 연결
        public EnemyFSM(EnemyBase enemy)
        {
            // 이 FSM을 생성한 Enemy의 객체 정보를 저장
            _enemy = enemy;

            // 상태 객체 생성
            Idle = new IdleState(enemy, this);
            Move = new MoveState(enemy, this);
            Attack = new AttackState(enemy, this);
            Dead = new DeadState(enemy, this);

            // Target 유효 여부 확인 객체 
            targetValidator = new TargetValidator(enemy, this);
            
            
            state = EnemyStateType.None;
            // FSM의 기본 시작 상태는 Idle 상태로 지정
            SetState(EnemyStateType.Idle);
        }

        // 상태 또는 외부 시스템의 상태 변경 요청
        public bool SetState(EnemyStateType next)
        {
            return Switch(next); // 중앙 상태 전환 결과 반환
        }


        /// <summary>
        /// EnemyStatus가 적의 체력이 0이 되었을 때, Dead 상태로 전이하도록 호출하는 함수
        /// </summary>
        public void SetDead()
        {
            SetState(EnemyStateType.Dead);
        }

        // 상태 종류에 맞는 고유 함수 실행
        private bool Switch(EnemyStateType next)
        {
            // 정의된 상태가 아니거나, 이전과 동일한 상태로 변경하려고 하면 false 반환
            if (!System.Enum.IsDefined(typeof(EnemyStateType), next) || next == state) return false;

            // 상태 설정
            state = next;

            // 상태에 진입사는 함수 호출
            switch (next)
            {
                case EnemyStateType.Idle:
                    Idle.Enter();
                    break;
                case EnemyStateType.Move:
                    Move.Enter();
                    break;
                case EnemyStateType.Attack:
                    Attack.Enter();
                    break;
                case EnemyStateType.Dead:
                    Dead.Enter();
                    break;
                // 정의되지 않은 상태의 경우, 실패를 반환
                default:
                    return false;
            }

            // 모든 조건을 통과하면 상태 변경 성공하였으므로 True 반환
            return true;
        }
    }
}
