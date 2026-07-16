using System.Collections.Generic;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;

namespace UJam.Runtime.BuildingPlacement
{
    public sealed class BuildingPlacementSystem
    {
        // 배치에 필요한 Grid 변환 계약
        private readonly IGridMetrics _gridMetrics;
        // 배치에 필요한 Grid 점유 계약
        private readonly IGridOccupancy _gridOccupancy;
        // Defense 생성 계약
        private readonly IDefenseFactory _defenseFactory;
        // 현재 예약 미리보기
        private PlacementPreview _currentPreview;
        // 성공 확정된 주문 식별자
        private readonly HashSet<string> _consumedOrderIds = new HashSet<string>();

        // 배치 시스템에 필요한 계약을 주입
        public BuildingPlacementSystem(IGridMetrics gridMetrics, IGridOccupancy gridOccupancy, IDefenseFactory defenseFactory)
        {
            // Grid 변환 계약을 저장
            _gridMetrics = gridMetrics;
            // Grid 점유 계약을 저장
            _gridOccupancy = gridOccupancy;
            // Defense 생성 계약을 저장
            _defenseFactory = defenseFactory;
        }

        // 현재 대기 중인 미리보기
        public PlacementPreview CurrentPreview
        {
            get
            {
                // 현재 미리보기를 반환
                return _currentPreview;
            }
        }

        // 대기 중인 배치 여부
        public bool HasPendingPlacement
        {
            get
            {
                // 미리보기 존재 여부를 반환
                return _currentPreview != null;
            }
        }

        // Preparation 배치를 시작하고 Grid를 예약
        public PlacementResult TryBeginPlacement(PurchaseOrder order, PhaseState phase, GridCell origin, GridFootprint footprint)
        {
            // 주문이 없으면 시작을 거부
            if (order == null)
            {
                // 주문 없음 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.NullOrder);
            }

            // Preparation이 아니면 시작을 거부
            if (phase != PhaseState.Preparation)
            {
                // 잘못된 Phase 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.InvalidPhase);
            }

            // 이미 미리보기가 있으면 중복 예약을 거부
            if (_currentPreview != null)
            {
                // 대기 배치 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.PendingPlacement);
            }

            // 이미 확정된 주문이면 재사용을 거부
            if (_consumedOrderIds.Contains(order.OrderId))
            {
                // 중복 주문 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.DuplicateOrder);
            }

            // 점유 계약이 없으면 예약을 거부
            if (_gridMetrics == null || _gridOccupancy == null || _defenseFactory == null)
            {
                // 의존성 누락 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.MissingDependency);
            }

            // Grid 점유를 한 번 요청해 검증과 예약을 함께 수행
            long reservationHandle;
            // 점유 성공 여부를 확인
            if (!_gridOccupancy.TryOccupy(origin, footprint, out reservationHandle))
            {
                // 점유 충돌 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.OccupancyConflict);
            }

            // 예약 핸들이 유효한지 확인
            if (reservationHandle <= 0)
            {
                // 잘못된 핸들은 즉시 해제
                _gridOccupancy.TryRelease(reservationHandle);
                // 점유 충돌 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.OccupancyConflict);
            }

            // 유효한 예약만 미리보기로 저장
            _currentPreview = new PlacementPreview(order, origin, footprint, reservationHandle);
            // 예약 결과를 반환
            return PlacementResult.Reserved(order, reservationHandle);
        }

        // 대기 중인 배치를 Defense 생성으로 확정
        public PlacementResult TryConfirmPlacement(PhaseState phase)
        {
            // 대기 배치가 없으면 확정을 거부
            if (_currentPreview == null)
            {
                // 미리보기 없음 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.MissingPreview);
            }

            // Preparation이 아니면 먼저 예약을 취소
            if (phase != PhaseState.Preparation)
            {
                // 취소된 배치 결과를 반환
                return ReleasePendingPlacement();
            }

            // 생성 의존성이 없으면 예약을 해제
            if (_gridMetrics == null || _defenseFactory == null)
            {
                // 의존성 누락을 위해 예약을 해제
                // 정확한 예약을 해제
                ReleasePendingReservation();
                // 의존성 누락 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.MissingDependency);
            }

            // 현재 미리보기로 Defense 생성 요청을 구성
            var preview = _currentPreview;
            // 월드 위치를 포함한 생성 요청을 생성
            var request = new DefenseSpawnRequest(preview.Order, preview.Origin, preview.Footprint, _gridMetrics, preview.ReservationHandle);
            // Defense factory를 정확히 한 번 호출
            var created = _defenseFactory.TryCreate(request);
            // Factory 결과를 확인
            if (!created)
            {
                // Factory 실패 시 예약을 해제
                ReleasePendingReservation();
                // Factory 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.FactoryFailed);
            }

            // 확정된 주문 식별자를 기록
            _consumedOrderIds.Add(preview.Order.OrderId);
            // 성공 시 핸들은 Defense가 소유하므로 로컬 상태만 삭제
            _currentPreview = null;
            // 확정 결과를 반환
            return PlacementResult.Confirmed(preview.Order, preview.ReservationHandle);
        }

        // 대기 중인 배치를 취소하고 예약을 해제
        public PlacementResult CancelPlacement()
        {
            // 대기 배치가 없으면 취소를 거부
            if (_currentPreview == null)
            {
                // 미리보기 없음 실패를 반환
                return PlacementResult.Failed(PlacementFailureReason.MissingPreview);
            }

            // 예약을 해제하고 취소 결과를 반환
            return ReleasePendingPlacement();
        }

        // Preparation 이탈 시 대기 배치를 취소
        public PlacementResult CancelForPhaseChange(PhaseState phase)
        {
            // Preparation에서는 예약을 유지
            if (phase == PhaseState.Preparation)
            {
                // 유지 결과를 반환
                return _currentPreview == null
                    ? PlacementResult.Failed(PlacementFailureReason.MissingPreview)
                    : PlacementResult.Reserved(_currentPreview.Order, _currentPreview.ReservationHandle);
            }

            // 다른 Phase에서는 대기 예약을 취소
            return CancelPlacement();
        }

        // 현재 예약 핸들을 해제하고 미리보기를 삭제
        private void ReleasePendingReservation()
        {
            // 현재 미리보기의 정확한 핸들을 보존
            var handle = _currentPreview.ReservationHandle;
            // Grid 점유를 해제
            _gridOccupancy?.TryRelease(handle);
            // 대기 상태를 삭제
            _currentPreview = null;
        }

        // 현재 예약을 해제하고 취소 결과를 생성
        private PlacementResult ReleasePendingPlacement()
        {
            // 취소 결과에 사용할 주문과 핸들을 보존
            var order = _currentPreview.Order;
            // 취소 결과에 사용할 핸들을 보존
            var handle = _currentPreview.ReservationHandle;
            // 정확한 예약을 해제
            ReleasePendingReservation();
            // 취소 결과를 반환
            return PlacementResult.Cancelled(order, handle);
        }
    }
}
