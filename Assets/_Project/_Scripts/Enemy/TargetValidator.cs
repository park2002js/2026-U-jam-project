using UnityEngine;
using UJam.Runtime.Grid;
using UJam.Runtime.Enemy.Movement;

namespace UJam.Runtime.Enemy
{
    /// <summary>
    /// Move 상태이거나, 혹은 Attack을 시작하기전 검증하기 위해 사용하는 객체이다.
    /// 우선 공격 대상이 사거리 내에 존재하는지 확인한다.
    /// 추가로 검증과정에서 Target을 담고 있는 Stack을 관리한다.
    /// </summary>
    public class TargetValidator
    {
        // 유효한지 확인하기 위한 Enemy 객체 정보
        private EnemyBase _enemy;

        // 우선 공격 대상 Stack을 보유하고 있는 EnemyFSM과 연결
        private EnemyFSM _fsm;

        // 생성자를 통해 객체 정보 연결
        public TargetValidator(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        /// <summary>
        /// 보유한 객체가 현재 타겟(공격 대상 스택의 Top)으로 하고 있는 대상이 유효한지 확인
        /// </summary>
        /// <returns>
        /// True면 우선 공격 대상이 범위 내에 존재한다는 것
        /// False면 우선 공격 대상이 범위 내에 없다는 것
        /// </returns>
        public bool Check()
        {
            // 1. EnemyFSM이 보유하고 있는 Stack을 직접 조회하고 유효성을 판단 
            GameObject target = null;
            while (_fsm.Targets.Count > 0)
            {
                target = _fsm.Targets[_fsm.Targets.Count - 1];

                // Top에 해당되는 대상이 "거점"이거나, 현재 게임 내에 존재한다면 스택 제거 과정 종료
                if (target != null && (target.CompareTag("BaseCore") || target.scene.IsValid() && target.scene.isLoaded)) break;

                // Top에 있는 대상이 현재 게임 내에 없다면 그대로 제거
                _fsm.Targets.RemoveAt(_fsm.Targets.Count - 1);
            }

            if (target == null || _fsm.Targets.Count == 0) return false;

            // 2. Enemy를 통해 EnemyStatus에서 사거리(칸 수) 및 GameObject의 Position을 가져옴
            Vector3 pos = _enemy.transform.position;
            float range = _enemy.Status.AttackRange;
            GridSystem grid = GridSystem.Instance;

            if (!grid.IsInitialized) return false;

            // 3-1. 우선 공격 대상이 거점인 경우 : 거점의 Row 줄의 중심 좌표의 Z축 좌표를 얻어내어, Enemy와의 Z축 좌표 차가 사거리 이내인지 확인
            if (target.CompareTag("BaseCore"))
            {
                float baseCoreZ = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
                return Mathf.Abs(pos.z - baseCoreZ) <= range * grid.CellHeight;
            }

            // 3-2. 우선 공격 대상이 거점이 아닌 경우 : 유클리드 거리로 사거리보다 안쪽에 존재하는지 확인
            Vector3 targetPos = target.transform.position;
            Vector2 toTarget = new Vector2(
                (targetPos.x - pos.x) / grid.CellWidth,
                (targetPos.z - pos.z) / grid.CellHeight);
            return toTarget.magnitude <= range;
        }
    }
}
