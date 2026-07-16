using UJam.Runtime.Shop;

namespace UJam.Runtime.BuildingPlacement
{
    public enum PlacementResultKind
    {
        Reserved,
        Confirmed,
        Cancelled,
        Failed
    }

    public enum PlacementFailureReason
    {
        NullOrder,
        InvalidPhase,
        PendingPlacement,
        DuplicateOrder,
        MissingDependency,
        OccupancyConflict,
        MissingPreview,
        FactoryFailed
    }

    public sealed class PlacementResult
    {
        // 배치 처리 결과를 생성
        private PlacementResult(
            PlacementResultKind kind,
            PlacementFailureReason? failureReason,
            PurchaseOrder order,
            long? reservationHandle)
        {
            // 결과 종류를 저장
            Kind = kind;
            // 실패 사유를 저장
            FailureReason = failureReason;
            // 배치 주문을 저장
            Order = order;
            // 점유 핸들을 저장
            ReservationHandle = reservationHandle;
        }

        // 배치 처리 결과의 종류
        public PlacementResultKind Kind { get; }

        // 실패 결과의 사유
        public PlacementFailureReason? FailureReason { get; }

        // 처리된 구매 주문
        public PurchaseOrder Order { get; }

        // 점유 핸들
        public long? ReservationHandle { get; }

        // 예약 결과를 생성
        public static PlacementResult Reserved(PurchaseOrder order, long reservationHandle)
        {
            // 예약 핸들은 양수만 허용
            if (reservationHandle <= 0)
            {
                // 잘못된 예약 핸들을 거부
                throw new System.ArgumentOutOfRangeException(nameof(reservationHandle));
            }

            // 예약 결과를 반환
            return new PlacementResult(PlacementResultKind.Reserved, null, order, reservationHandle);
        }

        // 확정 결과를 생성
        public static PlacementResult Confirmed(PurchaseOrder order, long reservationHandle)
        {
            // 확정 핸들은 양수만 허용
            if (reservationHandle <= 0)
            {
                // 잘못된 확정 핸들을 거부
                throw new System.ArgumentOutOfRangeException(nameof(reservationHandle));
            }

            // 확정 결과를 반환
            return new PlacementResult(PlacementResultKind.Confirmed, null, order, reservationHandle);
        }

        // 취소 결과를 생성
        public static PlacementResult Cancelled(PurchaseOrder order, long reservationHandle)
        {
            // 취소 핸들은 양수만 허용
            if (reservationHandle <= 0)
            {
                // 잘못된 취소 핸들을 거부
                throw new System.ArgumentOutOfRangeException(nameof(reservationHandle));
            }

            // 취소 결과를 반환
            return new PlacementResult(PlacementResultKind.Cancelled, null, order, reservationHandle);
        }

        // 실패 결과를 생성
        public static PlacementResult Failed(PlacementFailureReason failureReason)
        {
            // 실패 결과의 사유를 검증
            if (!System.Enum.IsDefined(typeof(PlacementFailureReason), failureReason))
            {
                // 정의되지 않은 실패 사유를 거부
                throw new System.ArgumentOutOfRangeException(nameof(failureReason));
            }

            // 실패 결과를 반환
            return new PlacementResult(PlacementResultKind.Failed, failureReason, null, null);
        }
    }
}
