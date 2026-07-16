using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationResult
    {
        // 상태별 부가 정보 조합을 검증하는 결과 생성자
        private NavigationResult(
            NavigationStatus status,
            ObstacleHandle blockedBy,
            GridCell attackPosition,
            NavigationFailureReason failureReason)
        {
            // 차단 상태에 공통 장애물 정보가 있는지 확인
            if (status == NavigationStatus.Blocked && !blockedBy.IsValid)
            {
                // 장애물 없는 차단 결과 생성 차단
                throw new ArgumentException("Blocked result requires a valid obstacle handle", nameof(blockedBy));
            }

            // 실패 상태에 실패 사유가 있는지 확인
            if (status == NavigationStatus.Failed && failureReason == NavigationFailureReason.None)
            {
                // 실패 사유 없는 실패 결과 생성 차단
                throw new ArgumentException("Failed result requires a failure reason", nameof(failureReason));
            }

            // 차단 상태가 아닌 결과에 장애물 정보가 없는지 확인
            if (status != NavigationStatus.Blocked && blockedBy.IsValid)
            {
                // 다른 상태의 장애물 payload 차단
                throw new ArgumentException("Only Blocked result can contain an obstacle handle", nameof(blockedBy));
            }

            // 실패 상태가 아닌 결과에 실패 사유가 없는지 확인
            if (status != NavigationStatus.Failed && failureReason != NavigationFailureReason.None)
            {
                // 다른 상태의 실패 사유 payload 차단
                throw new ArgumentException("Only Failed result can contain a failure reason", nameof(failureReason));
            }

            Status = status;
            BlockedBy = blockedBy;
            AttackPosition = attackPosition;
            FailureReason = failureReason;
        }

        // 현재 이동 상태
        public NavigationStatus Status { get; }

        // 차단을 일으킨 공통 장애물 Handle
        public ObstacleHandle BlockedBy { get; }

        // 차단 장애물에 대응할 공격 위치 Cell
        public GridCell AttackPosition { get; }

        // 실패 상태의 구체 사유
        public NavigationFailureReason FailureReason { get; }

        // 진행 중인 이동 결과 생성
        public static NavigationResult Moving()
        {
            // 이동 중 상태 결과 반환
            return new NavigationResult(
                NavigationStatus.Moving,
                default,
                default,
                NavigationFailureReason.None);
        }

        // 도착한 이동 결과 생성
        public static NavigationResult Arrived()
        {
            // 도착 상태 결과 반환
            return new NavigationResult(
                NavigationStatus.Arrived,
                default,
                default,
                NavigationFailureReason.None);
        }

        // 장애물로 차단된 이동 결과 생성
        public static NavigationResult Blocked(ObstacleHandle obstacleHandle, GridCell attackPosition)
        {
            // 차단 상태와 장애물 payload 반환
            return new NavigationResult(
                NavigationStatus.Blocked,
                obstacleHandle,
                attackPosition,
                NavigationFailureReason.None);
        }

        // 실패한 이동 결과 생성
        public static NavigationResult Failed(NavigationFailureReason failureReason)
        {
            // 실패 상태와 실패 사유 payload 반환
            return new NavigationResult(
                NavigationStatus.Failed,
                default,
                default,
                failureReason);
        }

        // 재탐색이 필요한 이동 결과 생성
        public static NavigationResult NeedsRepath()
        {
            // 재탐색 상태 결과 반환
            return new NavigationResult(
                NavigationStatus.NeedsRepath,
                default,
                default,
                NavigationFailureReason.None);
        }
    }
}
