using UnityEngine;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Enemy
{
    public class TargetValidator
    {
        private readonly EnemyBase _enemy;
        private readonly EnemyFSM _fsm;

        public TargetValidator(EnemyBase enemy, EnemyFSM fsm)
        {
            _enemy = enemy;
            _fsm = fsm;
        }

        public bool TryGetTarget(out GameObject target)
        {
            while (_fsm.Targets.Count > 0)
            {
                target = _fsm.Targets[_fsm.Targets.Count - 1];
                if (IsAvailable(target)) return true;
                _fsm.Targets.RemoveAt(_fsm.Targets.Count - 1);
            }
            target = null;
            return false;
        }

        public bool Check() => TryGetTarget(out GameObject target) && Check(target);

        public bool Check(GameObject target)
        {
            GridSystem grid = GridSystem.Instance;
            if (!IsAvailable(target) || grid == null || !grid.IsInitialized)
                return false;

            Vector3 pos = _enemy.transform.position;
            float range = _enemy.Status.AttackRange;
            if (target.CompareTag("BaseCore"))
            {
                float baseCoreZ = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
                return Mathf.Abs(pos.z - baseCoreZ) <= (range + 0.00001f) * grid.CellHeight;
            }

            Vector3 targetPos = target.transform.position;
            Vector2 toTarget = new Vector2(
                (targetPos.x - pos.x) / grid.CellWidth,
                (targetPos.z - pos.z) / grid.CellHeight);
            return toTarget.magnitude <= range + 0.00001f;
        }

        private static bool IsAvailable(GameObject target)
        {
            if (target == null || !target.activeInHierarchy || !target.scene.IsValid() || !target.scene.isLoaded)
                return false;
            EnemyBase enemy = target.GetComponent<EnemyBase>();
            return enemy == null || enemy.Status != null && enemy.Status.HP > 0f &&
                enemy.FSM != null && enemy.FSM.state != EnemyStateType.Dead;
        }
    }
}
