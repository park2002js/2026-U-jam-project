using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Enemy
{
    // 발표용 MVP에서 Enemy를 마지막 row 방향으로 직선 이동시키는 임시 Navigation
    // 발표 후 장애물과 Barricade와 A* 경로를 사용하는 정식 Navigation으로 전체 교체 대상
    public sealed class TempNavi
    {
        // 발표용 이동을 적용할 Enemy Transform
        private readonly Transform _enemyTransform;

        // FSM Move 상태에서만 임시 이동을 허용하는 값
        private bool _canMove;

        // 발표용 이동을 적용할 Enemy Transform 저장
        public TempNavi(Transform enemyTransform)
        {
            // 이동시킬 Transform 저장
            _enemyTransform = enemyTransform;
        }

        // 발표용 직선 Cell 이동 허용
        public void StartMovement()
        {
            // Move 상태의 임시 이동 허용
            _canMove = true;
        }

        // 발표용 직선 Cell 이동 중단
        public void StopMovement()
        {
            // Attack과 Dead 상태의 임시 이동 차단
            _canMove = false;
        }

        // 발표용으로 마지막 row에서 공격 사거리만큼 떨어진 Cell까지 직선 이동
        public bool Move(Vector2Int targetCell, int stopDistance, float speed)
        {
            // 임시 이동에 필요한 Transform과 Grid와 속도 확인
            GridSystem grid = GridSystem.Instance;
            if (!_canMove
                || _enemyTransform == null
                || !grid.IsInitialized
                || speed <= 0f
                || float.IsNaN(speed)
                || float.IsInfinity(speed))
            {
                // 임시 이동 실패 반환
                return false;
            }

            // 마지막 row에서 공격 사거리만큼 앞선 정지 row 계산
            int destinationRow = Mathf.Clamp(
                targetCell.y - Mathf.Max(0, stopDistance),
                0,
                grid.RowCount - 1);
            // Enemy가 생성된 col을 유지할 World x 좌표 계산
            float destinationX = grid.Origin.x + targetCell.x * grid.CellWidth;
            // 발표용 정지 row의 World z 좌표 계산
            float destinationZ = grid.Origin.z + destinationRow * grid.CellHeight;
            // 현재 높이를 유지한 임시 이동 목적지 구성
            Vector3 destination = new Vector3(destinationX, _enemyTransform.position.y, destinationZ);

            // 발표용 직선 이동 적용
            _enemyTransform.position = Vector3.MoveTowards(
                _enemyTransform.position,
                destination,
                speed * Time.deltaTime);

            // 임시 이동 적용 성공 반환
            return true;
        }
    }
}
