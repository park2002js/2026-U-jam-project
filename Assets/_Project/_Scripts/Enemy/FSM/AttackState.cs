using UJam.Runtime.Enemy;
using UnityEngine;
using System.Collections;

namespace UJam.Runtime.Enemy.FSM
{
    public class AttackState
    {
        // 공격 행동을 구현하는 Enemy
        private EnemyBase _enemy;

        private EnemyFSM _fsm;

        // 코루틴 종료를 위한 Coroutine
        private Coroutine coroutine;

        // 공격 대상을 바라보기 위해 저장하는 Target
        private GameObject target;

        // Enemy와 FSM 연결
        public AttackState(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        // Attack 반복을 진행하는 코루틴을 시작
        public void Enter()
        {
            // 지난 코루틴의 결과 초기화
            if(coroutine != null) _enemy.StopCoroutine(coroutine);
            target = null;

            // AttackState는 MonoBehaviour를 가지고 있지않으므로, 코루틴의 실행만 MonoBehaviour인 _enemy에게 맡김
            coroutine = _enemy.StartCoroutine(AttackLoop());
        }
        
        // Attack 상태를 빠져나오기 위한 코루틴
        public void Exit()
        {
            // 진행중인 코루틴이 있을 경우에만 종료
            if(coroutine != null)
            {
                _enemy.StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        private IEnumerator AttackLoop()
        {
            // TargetValidator가 True를 내보내면 계속 공격을 진행
            while (_fsm.state == EnemyStateType.Attack && _fsm.TGV.Check())
            {
                // 공격 대상이 달라졌다면 그 대상을 바라보도록 Enemy의 Y축 방향을 조정
                if(target != _fsm.Targets[_fsm.Targets.Count - 1])
                {
                    target = _fsm.Targets[_fsm.Targets.Count - 1];

                    // 거점 공격은 월드 +Z 정면(Y 회전 0)을 유지하고 다른 대상만 실제 위치를 바라본다.
                    Vector3 direction = target.CompareTag("BaseCore") ? Vector3.forward : target.transform.position - _enemy.transform.position;

                    // Y축을 기준으로 회전하므로 벡터의 Y축 변화량은 0으로 설정함
                    direction.y = 0f;    
                    if (direction.sqrMagnitude > 0f)
                    {
                        _enemy.transform.rotation = Quaternion.LookRotation(direction);
                    }
                }


                float attackSpeed = _enemy.Status.AttackSpeed;

                // 공격 속도가 0이면 공격하지 않고 다음 프레임에 다시 확인
                if (attackSpeed <= 0f)
                {
                    yield return null;
                    continue;
                }

                // 지연되는 반복은 Attack State 내에서, 실제 동작은 EnemyBase의 Attack에서 구현.
                _enemy.Attack();

                // 공격 속도는 초당 공격 횟수이므로 이를 고려하여 지연을 결정
                yield return new WaitForSeconds(1f / attackSpeed);
            }

            // 반복문이 종료되면 False를 내보낸 것이므로 Move 상태로 변경
            if (_fsm.state == EnemyStateType.Attack)
            {
                _fsm.SetState(EnemyStateType.Move);
                yield break;
            }
        }
    }
}
