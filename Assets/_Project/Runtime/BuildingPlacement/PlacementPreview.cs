using UJam.Runtime.Grid;
using UJam.Runtime.Shop;

namespace UJam.Runtime.BuildingPlacement
{
    public sealed class PlacementPreview
    {
        // 배치 미리보기 데이터를 생성
        public PlacementPreview(PurchaseOrder order, GridCell origin, GridFootprint footprint, long reservationHandle)
        {
            // 주문이 없으면 미리보기를 만들 수 없음
            if (order == null)
            {
                // 잘못된 주문을 거부
                throw new System.ArgumentNullException(nameof(order));
            }

            // 미리보기 핸들은 양수만 허용
            if (reservationHandle <= 0)
            {
                // 잘못된 미리보기 핸들을 거부
                throw new System.ArgumentOutOfRangeException(nameof(reservationHandle));
            }

            // 미리보기 주문을 저장
            Order = order;
            // 미리보기 원점을 저장
            Origin = origin;
            // 미리보기 영역을 저장
            Footprint = footprint;
            // 미리보기 점유 핸들을 저장
            ReservationHandle = reservationHandle;
        }

        // 대기 중인 구매 주문
        public PurchaseOrder Order { get; }

        // 배치 원점 셀
        public GridCell Origin { get; }

        // 배치 영역
        public GridFootprint Footprint { get; }

        // 정확한 점유 핸들
        public long ReservationHandle { get; }
    }
}
