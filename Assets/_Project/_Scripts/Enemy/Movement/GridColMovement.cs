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

        /// <summary>
        /// Enemy가 Move 상태에 진입했을 때, 실제 이동을 구현하기 위해 호출하는 함수
        /// </summary>
        public override void Enter()
        {
            if (_enemyBase != null) StartCoroutine(MoveUntilInRange());
        }

        /// <summary>
        /// Enemy가 Move 상태인 동안 반복 호출되는 함수
        /// 이동을 실제로 구현해야 한다.
        /// </summary>
        public override void Tick()
        {
        }

        /// <summary>
        /// Enemy가 Move 상태에서 벗어났을 때, 이동관련 로직을 마무리하기 위해 호출하는 함수
        /// </summary>
        public override void Exit()
        {
        }

        private IEnumerator MoveUntilInRange()
        {
            GridSystem grid = GridSystem.Instance;
            if (!grid.IsInitialized) yield break;

            while (_enemyBase.FSM.state == EnemyStateType.Move)
            {
                GameObject target = null;
                while (_enemyBase.FSM.Targets.Count > 0)
                {
                    target = _enemyBase.FSM.Targets[_enemyBase.FSM.Targets.Count - 1];
                    if (target != null
                        && (target.CompareTag("BaseCore") || target.scene.IsValid() && target.scene.isLoaded)) break;

                    _enemyBase.FSM.Targets.RemoveAt(_enemyBase.FSM.Targets.Count - 1);
                }

                if (target == null || _enemyBase.FSM.Targets.Count == 0) yield break;

                Vector3 pos = _enemyBase.transform.position;
                float range = _enemyBase.Status.AttackRange;
                float moveCells = _enemyBase.Status.Speed * Time.deltaTime;
                bool reachedRange;

                if (target.CompareTag("BaseCore"))
                {
                    float targetZ = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
                    float distanceToRange = Mathf.Abs(targetZ - pos.z) / grid.CellHeight - range;
                    reachedRange = distanceToRange <= moveCells;

                    if (distanceToRange > 0f)
                    {
                        pos.z += Mathf.Sign(targetZ - pos.z)
                            * Mathf.Min(moveCells, distanceToRange) * grid.CellHeight;
                    }
                }
                else
                {
                    Vector3 targetPos = target.transform.position;
                    Vector2 toTarget = new Vector2(
                        (targetPos.x - pos.x) / grid.CellWidth,
                        (targetPos.z - pos.z) / grid.CellHeight);
                    float manhattanDistance = Mathf.Abs(toTarget.x) + Mathf.Abs(toTarget.y);
                    float distanceToRange = manhattanDistance <= range
                        ? 0f
                        : toTarget.magnitude * (manhattanDistance - range) / manhattanDistance;
                    reachedRange = distanceToRange <= moveCells;
                    Vector2 movement = toTarget.normalized * Mathf.Min(moveCells, distanceToRange);

                    pos.x += movement.x * grid.CellWidth;
                    pos.z += movement.y * grid.CellHeight;
                }

                _enemyBase.transform.position = pos;

                if (reachedRange)
                {
                    _enemyBase.FSM.SetState(EnemyStateType.Attack);
                    yield break;
                }

                yield return null;
            }
        }
    }
}
