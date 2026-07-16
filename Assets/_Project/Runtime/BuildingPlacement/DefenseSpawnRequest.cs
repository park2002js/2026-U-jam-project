using UnityEngine;
using UJam.Runtime.Grid;
using UJam.Runtime.Shop;

namespace UJam.Runtime.BuildingPlacement
{
    public sealed class DefenseSpawnRequest
    {
        // Defense 생성 요청 데이터를 생성
        public DefenseSpawnRequest(
            PurchaseOrder order,
            GridCell origin,
            GridFootprint footprint,
            IGridMetrics gridMetrics,
            long reservationHandle)
        {
            // 주문이 없으면 생성 요청을 만들 수 없음
            if (order == null)
            {
                // 잘못된 주문을 거부
                throw new System.ArgumentNullException(nameof(order));
            }

            // Grid Metrics가 없으면 위치를 계산할 수 없음
            if (gridMetrics == null)
            {
                // 잘못된 Grid Metrics를 거부
                throw new System.ArgumentNullException(nameof(gridMetrics));
            }

            // 생성 요청 핸들은 양수만 허용
            if (reservationHandle <= 0)
            {
                // 잘못된 생성 요청 핸들을 거부
                throw new System.ArgumentOutOfRangeException(nameof(reservationHandle));
            }

            // 생성 요청 주문을 저장
            Order = order;
            // 생성 요청 원점을 저장
            Origin = origin;
            // 생성 요청 영역을 저장
            Footprint = footprint;
            // Grid Metrics로 월드 위치를 계산해 저장
            WorldPosition = gridMetrics.CellToWorld(origin);
            // 생성 요청 점유 핸들을 저장
            ReservationHandle = reservationHandle;
        }

        // Defense 생성에 사용할 구매 주문
        public PurchaseOrder Order { get; }

        // Defense 생성 원점 셀
        public GridCell Origin { get; }

        // Defense 생성 영역
        public GridFootprint Footprint { get; }

        // Defense 생성 월드 위치
        public Vector3 WorldPosition { get; }

        // Defense가 이어받을 점유 핸들
        public long ReservationHandle { get; }
    }
}
