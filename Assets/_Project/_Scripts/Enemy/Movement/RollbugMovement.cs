using System.Collections;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Enemy.Movement
{
    public class RollbugMovement : EnemyMovement
    {
        [SerializeField] private float _leftWall = -16f;
        [SerializeField] private float _rightWall = 16f;
        [SerializeField, Range(1f, 65f)] private float _minimumDiagonalAngle = 25f;
        private Coroutine _routine;
        private GameObject _target;
        private Vector3 _direction;
        private Collider _collider;

        public override void Enter()
        {
            if (_enemyBase == null) return;
            Exit();
            _collider = _enemyBase.GetComponent<Collider>();
            ChooseDirection();
            _routine = StartCoroutine(MoveLoop());
        }

        public override void Exit()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
        }

        public override void ReTargeting()
        {
            ChooseDirection();
        }

        private void ChooseDirection()
        {
            if (_enemyBase.FSM.Targets.Count == 0) return;
            GameObject next = _enemyBase.FSM.Targets[_enemyBase.FSM.Targets.Count - 1];
            // Re-entering after knockback keeps a reflected heading; an actual target change may re-aim.
            if (next == _target && _direction.sqrMagnitude > 0f) return;
            _target = next;
            if (_target == null) return;
            Vector3 delta = _target.transform.position - _enemyBase.transform.position;
            if (_target.CompareTag("BaseCore") && GridSystem.Instance != null)
                delta.z = GridSystem.Instance.Origin.z + GridSystem.Instance.BaseCoreRow * GridSystem.Instance.CellHeight - transform.position.z;
            float side = Mathf.Abs(delta.x) > 0.001f ? Mathf.Sign(delta.x) : (transform.position.x > 0f ? -1f : 1f);
            float angle = Mathf.Clamp(Mathf.Atan2(Mathf.Abs(delta.x), Mathf.Abs(delta.z)) * Mathf.Rad2Deg, _minimumDiagonalAngle, 65f) * Mathf.Deg2Rad;
            _direction = new Vector3(Mathf.Sin(angle) * side, 0f, Mathf.Cos(angle));
            _enemyBase.transform.rotation = Quaternion.LookRotation(_direction);
        }

        private IEnumerator MoveLoop()
        {
            while (_enemyBase.FSM.state == EnemyStateType.Move)
            {
                GridSystem grid = GridSystem.Instance;
                if (grid == null || !grid.IsInitialized || _enemyBase.Status.IsStunned)
                {
                    yield return null;
                    continue;
                }
                if (_enemyBase.FSM.TGV.Check())
                {
                    _routine = null;
                    _enemyBase.FSM.SetState(EnemyStateType.Attack);
                    yield break;
                }
                ChooseDirection();
                if (_target == null || _direction.sqrMagnitude == 0f) { yield return null; continue; }
                float halfWidth = HorizontalExtent();
                float minX = _leftWall + halfWidth;
                float maxX = _rightWall - halfWidth;
                if (minX >= maxX)
                {
                    Debug.LogError($"{name}의 벽 사이 공간보다 충돌체가 큽니다.", this);
                    yield break;
                }
                Vector3 position = _enemyBase.transform.position;
                position.x = Mathf.Clamp(position.x, minX, maxX);
                float remaining = _enemyBase.Status.Speed * grid.CellHeight * Time.deltaTime;
                while (remaining > 0f)
                {
                    float wall = _direction.x > 0f ? maxX : minX;
                    float toWall = Mathf.Max(0f, (wall - position.x) / _direction.x);
                    float distance = Mathf.Min(remaining, toWall);
                    Vector3 next = position + _direction * distance;
                    if (TryEnterAttackRange(position, next, grid, out float fraction))
                    {
                        _enemyBase.transform.position = Vector3.Lerp(position, next, fraction);
                        _routine = null;
                        _enemyBase.FSM.SetState(EnemyStateType.Attack);
                        yield break;
                    }
                    position = next;
                    remaining -= distance;
                    if (distance >= toWall)
                    {
                        position.x = wall;
                        ReflectPosition(ref position, ref _direction, minX, maxX);
                        _enemyBase.transform.rotation = Quaternion.LookRotation(_direction);
                    }
                }
                _enemyBase.transform.position = position;
                yield return null;
            }
            _routine = null;
        }

        private bool TryEnterAttackRange(Vector3 from, Vector3 to, GridSystem grid, out float fraction)
        {
            fraction = 0f;
            if (_target == null) return false;
            float range = _enemyBase.Status.AttackRange;
            if (_target.CompareTag("BaseCore"))
            {
                float z = grid.Origin.z + grid.BaseCoreRow * grid.CellHeight;
                float minZ = z - range * grid.CellHeight;
                float maxZ = z + range * grid.CellHeight;
                if (from.z >= minZ && from.z <= maxZ) return true;
                if (to.z < minZ || from.z > maxZ || to.z == from.z) return false;
                fraction = Mathf.Clamp01((minZ - from.z) / (to.z - from.z));
                return true;
            }
            Vector3 target = _target.transform.position;
            return TryRangeEntry(new Vector2((from.x - target.x) / grid.CellWidth, (from.z - target.z) / grid.CellHeight),
                new Vector2((to.x - target.x) / grid.CellWidth, (to.z - target.z) / grid.CellHeight), range, out fraction);
        }

        private float HorizontalExtent()
        {
            if (_collider is BoxCollider box)
            {
                Vector3 scale = _enemyBase.transform.lossyScale;
                float x = (Mathf.Abs(box.center.x) + box.size.x * 0.5f) * Mathf.Abs(scale.x);
                float z = (Mathf.Abs(box.center.z) + box.size.z * 0.5f) * Mathf.Abs(scale.z);
                // A fixed footprint keeps the rotated collider inside the wall during reflection.
                return Mathf.Sqrt(x * x + z * z);
            }
            return _collider != null
                ? _collider.bounds.extents.x + Mathf.Abs(_collider.bounds.center.x - _enemyBase.transform.position.x)
                : 0f;
        }

        public static bool TryRangeEntry(Vector2 from, Vector2 to, float range, out float fraction)
        {
            fraction = 0f;
            float c = from.sqrMagnitude - range * range;
            if (c <= 0f) return true;
            Vector2 step = to - from;
            float a = step.sqrMagnitude;
            if (a == 0f) return false;
            float b = 2f * Vector2.Dot(from, step);
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f) return false;
            fraction = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            return fraction >= 0f && fraction <= 1f;
        }

        public static void ReflectPosition(ref Vector3 position, ref Vector3 direction, float minX, float maxX)
        {
            if (minX >= maxX) return;
            while (position.x < minX || position.x > maxX ||
                (position.x <= minX && direction.x < 0f) || (position.x >= maxX && direction.x > 0f))
            {
                float wall = position.x <= minX ? minX : maxX;
                position.x = 2f * wall - position.x;
                direction.x = -direction.x;
            }
        }

        private void OnDisable() => Exit();
    }
}
