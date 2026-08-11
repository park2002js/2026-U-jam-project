using System.Collections;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Enemy.Movement
{
    /// <summary>
    /// 자신이 생성된 위치의 Column을 유지한 채, Row 칸을 증가시키면서 이동하는 Movement 로직
    /// 플레이어 입장에서는 적이 화면 상단에서 수직으로 내려오는 것처럼 보임
    /// </summary>
    public class GridColMovement : EnemyMovement
    {

        // 현재 코루틴이 활성화되어 목적지로 이동중인지 여부를 나타내는 Bool 변수
        private bool isMoving;

        // 현재 진행중인 코루틴을 저장할 변수
        private Coroutine coroutine;

        // Grid System의 데이터를 쓰기 위해 저장하는 인스턴스
        private GridSystem grid;

        // Move의 목적지를 계산할 우선 공격 대상
        private GameObject target;

        // 우선 공격 대상을 Attack 하기 위한 목적지에 해당하는 좌표
        private Vector3 dest;

        /// <summary>
        /// Enemy가 Move 상태에 진입했을 때, 실제 이동을 구현하기 위해 호출하는 함수
        /// </summary>
        public override void Enter()
        {
            // init이 되지 않았는데 Enter로 Movement 발동이 되는 것을 방지
            if (_enemyBase == null) return;

            // 새로운 코루틴을 생성하기 위해 기존에 진행중인 코루틴이 있다면 제거
            if (coroutine != null) StopCoroutine(coroutine);

            // 거리 계산을 위해 필요한 정보들 불러옴
            grid = GridSystem.Instance;                                         // 거리 계산을 위해 Grid System 객체 사용
            target = _enemyBase.FSM.Targets[_enemyBase.FSM.Targets.Count - 1];  // 현재 우선 공격 대상을 Stack에서 가져옴, Enter가 되기전에 TargetValidator가 발동된 상황

            Vector3 pos = _enemyBase.transform.position;    // Enemy의 현재 위치
            float range = _enemyBase.Status.AttackRange;    // Enemy의 사거리
            dest = pos; // 변화하는 값(X축, Z축) 외의 수치들(Y축)은 유지시키기 위해 '목적지 좌표'(dest)에 일단 복사

            // 공격 대상이 "거점"일 경우 Z축 거리 차이만 계산
            if (target.CompareTag("BaseCore"))
            {
                // 거점이 있는 Row 줄 중심의 Z축 좌표를 얻은 뒤, 목적지로부터 사거리만큼 수직거리로 떨어진 Z축 좌표를 얻어서 목적지로 설정
                float targetZ = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
                dest.z = targetZ - Mathf.Sign(targetZ - pos.z) * range * grid.CellHeight;
            }
            else // 공격대상이 거점이 아닌 경우, 유클리드 거리로 목적지 위치 계산
            {
                // 현재 위치에서 타겟 좌표로 향하는 벡터를 계산함 : 타겟 위치 점 - 현 위치 점
                Vector3 targetPos = target.transform.position;
                Vector2 toTarget = new Vector2( (targetPos.x - pos.x) / grid.CellWidth, (targetPos.z - pos.z) / grid.CellHeight); // Range 계산이 있으므로 Grid 단위로 변경

                // 벡터길이(magnitude)에서 사거리만큼을 제해서 움직어야 할 거리를 계산한다. 사거리 이내일 경우 움직이지 말아야 하므로 Max(0, ~)로 거리를 계산한다.
                float moveDistance = Mathf.Max(0f, toTarget.magnitude - range);

                // 얻은 길이 (스칼라 값)을 현위치에서 타겟으로 향하는 벡터를 정규화 한 것과 곱해서, 최종적으로 도착해야 할 위치 좌표를 구함
                Vector2 movement = toTarget.normalized * moveDistance;

                // Speed와 Range가 Grid 칸 수를 기준으로 값이 사용되었으므로, 최종적으로 목표 좌표를 "칸 단위"를 "월드 거리"로 변환하여 저장
                dest.x += movement.x * grid.CellWidth;
                dest.z += movement.y * grid.CellHeight;
            }

            isMoving = true;
            coroutine = StartCoroutine(MoveUntilInRange());
        }

        /// <summary>
        /// Enemy가 Move 상태에서 벗어났을 때, 이동관련 로직을 마무리하기 위해 호출하는 함수
        /// </summary>
        public override void Exit()
        {
            if (!isMoving && coroutine != null)
            {
                isMoving = false;
                StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        /// <summary>
        /// EnemyFSM측에서 공격 대상이 달라졌는지 여부를 확인해야 할 때 호출하는 부분
        /// </summary>
        public override void ReTargeting()
        {
            if (_enemyBase == null) return;

            // 만약에 사거리 내에 존재하지 않는데, 공격 대상이 달라졋을 경우 다시 거리 계산을 해야 하므로 Enter로 이동
            GameObject nextTarget = _enemyBase.FSM.Targets[_enemyBase.FSM.Targets.Count - 1];
            if (nextTarget != target) Enter();
        }

        private IEnumerator MoveUntilInRange()
        {
            // 코루틴 내에서 매 프레임마다 움직임을 반복
            while (isMoving)
            {
                // Enemy의 현 정보를 얻어옴 : 현 위치 월드 좌표, 이번 프레임에 이동할 칸 수 (=초당 이동 칸수 * deltaTime)
                Vector3 pos = _enemyBase.transform.position;
                float moveCells = _enemyBase.Status.Speed * Time.deltaTime;

                // 현재 위치와 목표 Grid 좌표를 얻어냄
                Vector2 currentGridPos = new Vector2(pos.x / grid.CellWidth, pos.z / grid.CellHeight);
                Vector2 destinationGridPos = new Vector2(dest.x / grid.CellWidth, dest.z / grid.CellHeight);

                // 이번 프레임 이동으로 목표 거리 이내로 이동하게 된다면, 목표 지점으로 보정한 뒤 Attack 상태로 변경
                if (Vector2.Distance(currentGridPos, destinationGridPos) <= moveCells)
                {
                    pos.x = dest.x; pos.z = dest.z;
                    _enemyBase.transform.position = pos;
                    isMoving = false;
                    coroutine = null;

                    // 온전히 목표 지점까지 이동했으므로, 자동으로 공격 상태로 전환
                    _enemyBase.FSM.SetState(EnemyStateType.Attack);
                    yield break;
                }

                // 아직 목표지점까지 남아있다면, 이번 프레임에 이동할 거리(칸 수)만큼 현재 벡터 방향으로 Position을 변경함
                Vector2 nextGridPos = Vector2.MoveTowards(currentGridPos, destinationGridPos, moveCells);
                pos.x = nextGridPos.x * grid.CellWidth;
                pos.z = nextGridPos.y * grid.CellHeight;
                _enemyBase.transform.position = pos;

                // 다음 프레임까지 코루틴 정지 -> 매 프레임 이동을 구현
                yield return null;
            }
        }
    }
}
