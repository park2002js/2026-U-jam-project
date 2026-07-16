using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationMotorResult
    {
        // Motor 상태 저장
        private readonly NavigationStatus _status;
        // 차단 장애물 Handle 저장
        private readonly ObstacleHandle _blockedBy;
        // 차단 대응 공격 위치 저장
        private readonly GridCell _attackPosition;
        // Motor 실패 사유 저장
        private readonly NavigationFailureReason _failureReason;
        // Factory로 생성된 유효한 Motor 결과인지 저장
        private readonly bool _isValid;

        // 상태별 Motor payload를 검증하고 저장
        private NavigationMotorResult(
            NavigationStatus status,
            ObstacleHandle blockedBy,
            GridCell attackPosition,
            NavigationFailureReason failureReason)
        {
            // 차단 상태는 유효한 장애물 Handle을 요구
            if (status == NavigationStatus.Blocked && !blockedBy.IsValid)
            {
                throw new ArgumentException("Blocked motor result requires a valid obstacle handle", nameof(blockedBy));
            }

            // 실패 상태는 승인된 실패 사유를 요구
            if (status == NavigationStatus.Failed && failureReason == NavigationFailureReason.None)
            {
                throw new ArgumentException("Failed motor result requires a failure reason", nameof(failureReason));
            }

            // 차단 상태 외에는 장애물 Handle을 금지
            if (status != NavigationStatus.Blocked && blockedBy.IsValid)
            {
                throw new ArgumentException("Only blocked motor result can contain an obstacle handle", nameof(blockedBy));
            }

            // 실패 상태 외에는 실패 사유를 금지
            if (status != NavigationStatus.Failed && failureReason != NavigationFailureReason.None)
            {
                throw new ArgumentException("Only failed motor result can contain a failure reason", nameof(failureReason));
            }

            _status = status;
            _blockedBy = blockedBy;
            _attackPosition = attackPosition;
            _failureReason = failureReason;
            _isValid = true;
        }

        // Factory로 생성된 유효한 결과 여부
        public bool IsValid
        {
            get
            {
                // 기본 struct와 구분되는 결과 유효성 반환
                return _isValid;
            }
        }

        // Motor 상태
        public NavigationStatus Status
        {
            get
            {
                // 저장된 Motor 상태 반환
                return _status;
            }
        }

        // 차단 장애물 Handle
        public ObstacleHandle BlockedBy
        {
            get
            {
                // 저장된 장애물 Handle 반환
                return _blockedBy;
            }
        }

        // 차단 대응 공격 위치
        public GridCell AttackPosition
        {
            get
            {
                // 저장된 공격 위치 반환
                return _attackPosition;
            }
        }

        // Motor 실패 사유
        public NavigationFailureReason FailureReason
        {
            get
            {
                // 저장된 실패 사유 반환
                return _failureReason;
            }
        }

        // 이동 중 Motor 결과 생성
        public static NavigationMotorResult Moving()
        {
            // 이동 중 결과 반환
            return new NavigationMotorResult(
                NavigationStatus.Moving,
                default,
                default,
                NavigationFailureReason.None);
        }

        // 도착 Motor 결과 생성
        public static NavigationMotorResult Arrived()
        {
            // 도착 결과 반환
            return new NavigationMotorResult(
                NavigationStatus.Arrived,
                default,
                default,
                NavigationFailureReason.None);
        }

        // 차단 Motor 결과 생성
        public static NavigationMotorResult Blocked(ObstacleHandle blockedBy, GridCell attackPosition)
        {
            // 차단 payload를 포함한 결과 반환
            return new NavigationMotorResult(
                NavigationStatus.Blocked,
                blockedBy,
                attackPosition,
                NavigationFailureReason.None);
        }

        // 실패 Motor 결과 생성
        public static NavigationMotorResult Failed(NavigationFailureReason failureReason)
        {
            // 실패 사유를 포함한 결과 반환
            return new NavigationMotorResult(
                NavigationStatus.Failed,
                default,
                default,
                failureReason);
        }

        // 재탐색 Motor 결과 생성
        public static NavigationMotorResult NeedsRepath()
        {
            // 재탐색 결과 반환
            return new NavigationMotorResult(
                NavigationStatus.NeedsRepath,
                default,
                default,
                NavigationFailureReason.None);
        }
    }
}
