using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    // 발표용 MVP에서 마지막 row 거점까지의 임시 격자 거리를 판단하는 객체
    // 발표 후 장애물과 실제 경로 비용을 반영하는 Navigation 거리 판정으로 교체 대상
    public sealed class EnemyRangeChecker
    {
        // 임시 거리 판정에 사용할 Enemy Transform
        private readonly Transform _enemyTransform;

        // 공격 가능 여부를 판단할 기본 거점 객체
        private GameObject _target;

        // 임시 거리 판정에 사용할 Enemy Transform 저장
        public EnemyRangeChecker(Transform enemyTransform)
        {
            // 거리 계산 주체 저장
            _enemyTransform = enemyTransform;
        }

        // 발표용 기본 거점 Target 교체
        public void SetTarget(GameObject target)
        {
            // 이후 거리 판정에서 사용할 거점 객체 저장
            _target = target;
        }

        // 발표용 마지막 row와 Enemy 사이의 Grid row 거리 판단
        public bool TryCheckRange(
            int attackRangeCellCount,
            out bool inRange,
            out Vector2Int targetCell,
            out Vector3 targetPoint)
        {
            // 실패 시 사용할 사거리 밖 상태
            inRange = false;
            // 실패 시 사용할 빈 목표 Cell
            targetCell = default;
            // 실패 시 사용할 빈 공격 지점
            targetPoint = default;

            // 임시 판정에 필요한 Enemy와 거점과 Grid 준비 확인
            GridSystem grid = GridSystem.Instance;
            if (_enemyTransform == null || _target == null || !grid.IsInitialized)
            {
                // 임시 거리 판정 실패 반환
                return false;
            }

            // Enemy의 현재 World 위치를 Grid 좌표로 변환
            if (!TryGetCell(_enemyTransform.position, grid, out Vector2Int enemyCell))
            {
                // Grid 밖 Enemy의 임시 거리 판정 실패 반환
                return false;
            }

            // 발표용 거점이 차지하는 마지막 row에서 Enemy와 같은 col 선택
            int targetRow = grid.RowCount - 1;
            // 거점의 현재 공격 목표 Cell 구성
            targetCell = new Vector2Int(enemyCell.x, targetRow);
            // 같은 col의 마지막 row를 실제 공격 World 지점으로 변환
            targetPoint = new Vector3(
                grid.Origin.x + targetCell.x * grid.CellWidth,
                _target.transform.position.y,
                grid.Origin.z + targetCell.y * grid.CellHeight);
            // Enemy의 현재 World z를 연속된 Grid row 단위로 변환
            float enemyRowPosition = (_enemyTransform.position.z - grid.Origin.z) / grid.CellHeight;
            // 마지막 row까지 남은 임시 Grid row 거리 계산
            float distance = Mathf.Abs(targetRow - enemyRowPosition);
            // 음수 공격 사거리를 0으로 보정
            int safeRange = Mathf.Max(0, attackRangeCellCount);

            // 발표용 거리 계산이 음수가 아님을 실행 중 확인
            Debug.Assert(distance >= 0, "Temporary enemy grid distance became negative.");

            // 정해진 Grid 칸 이내인지 저장
            inRange = distance <= safeRange + 0.0001f;

            // 임시 거리 판정 성공 반환
            return true;
        }

        // 발표용 World 위치를 현재 Grid Cell로 변환
        private static bool TryGetCell(Vector3 worldPosition, GridSystem grid, out Vector2Int cell)
        {
            // 실패 시 사용할 빈 Cell
            cell = default;
            // World 위치에서 col 계산
            int col = Mathf.RoundToInt((worldPosition.x - grid.Origin.x) / grid.CellWidth);
            // World 위치에서 row 계산
            int row = Mathf.RoundToInt((worldPosition.z - grid.Origin.z) / grid.CellHeight);

            // Grid 범위 밖 위치 차단
            if (row < 0 || row >= grid.RowCount || col < 0 || col >= grid.ColumnCount)
            {
                // Cell 변환 실패 반환
                return false;
            }

            // x는 col이고 y는 row인 Cell 저장
            cell = new Vector2Int(col, row);

            // Cell 변환 성공 반환
            return true;
        }
    }
}
