using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationPathResult
    {
        // 성공 경로 여부 저장
        private readonly bool _isSuccess;
        // 차단 결과 여부 저장
        private readonly bool _isBlocked;
        // 성공 경로 payload 저장
        private readonly NavigationPath _path;
        // 차단 장애물 Handle 저장
        private readonly ObstacleHandle _blockedBy;
        // 차단 대응 공격 위치 저장
        private readonly GridCell _attackPosition;
        // 경로 실패 사유 저장
        private readonly NavigationFailureReason _failureReason;

        // 상태별 경로 결과 payload를 검증하고 저장
        private NavigationPathResult(
            bool isSuccess,
            bool isBlocked,
            NavigationPath path,
            ObstacleHandle blockedBy,
            GridCell attackPosition,
            NavigationFailureReason failureReason)
        {
            // 성공과 차단을 동시에 반환하지 않는지 확인
            if (isSuccess && isBlocked)
            {
                throw new ArgumentException("Path result cannot be success and blocked", nameof(isBlocked));
            }

            // 차단 결과는 유효한 장애물 Handle을 요구
            if (isBlocked && !blockedBy.IsValid)
            {
                throw new ArgumentException("Blocked path requires a valid obstacle handle", nameof(blockedBy));
            }

            // 실패 결과는 승인된 실패 사유를 요구
            if (!isSuccess && !isBlocked && failureReason == NavigationFailureReason.None)
            {
                throw new ArgumentException("Failed path requires a failure reason", nameof(failureReason));
            }

            // 성공 결과 외에는 경로 객체를 사용하지 않도록 제한
            if (!isSuccess && path.Cells != null)
            {
                throw new ArgumentException("Only success path can contain path cells", nameof(path));
            }

            // 성공 결과는 실제 경로 Cell 목록을 요구
            if (isSuccess && path.Cells == null)
            {
                throw new ArgumentException("Success path requires path cells", nameof(path));
            }

            // 차단 결과 외에는 장애물 Handle을 사용하지 않도록 제한
            if (!isBlocked && blockedBy.IsValid)
            {
                throw new ArgumentException("Only blocked path can contain an obstacle handle", nameof(blockedBy));
            }

            // 실패 결과 외에는 실패 사유를 사용하지 않도록 제한
            if ((isSuccess || isBlocked) && failureReason != NavigationFailureReason.None)
            {
                throw new ArgumentException("Only failed path can contain a failure reason", nameof(failureReason));
            }

            _isSuccess = isSuccess;
            _isBlocked = isBlocked;
            _path = path;
            _blockedBy = blockedBy;
            _attackPosition = attackPosition;
            _failureReason = failureReason;
        }

        // 성공 경로 결과 여부
        public bool IsSuccess
        {
            get
            {
                // 성공 경로 여부 반환
                return _isSuccess;
            }
        }

        // 차단 경로 결과 여부
        public bool IsBlocked
        {
            get
            {
                // 차단 결과 여부 반환
                return _isBlocked;
            }
        }

        // 실패 경로 결과 여부
        public bool IsFailed
        {
            get
            {
                // 실패 사유가 있는 결과 여부 반환
                return !_isSuccess && !_isBlocked && _failureReason != NavigationFailureReason.None;
            }
        }

        // 성공 경로 payload
        public NavigationPath Path
        {
            get
            {
                // 저장된 경로 반환
                return _path;
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

        // 경로 실패 사유
        public NavigationFailureReason FailureReason
        {
            get
            {
                // 저장된 실패 사유 반환
                return _failureReason;
            }
        }

        // 성공 경로 결과 생성
        public static NavigationPathResult Succeeded(NavigationPath path)
        {
            // 성공 경로 결과 반환
            return new NavigationPathResult(
                true,
                false,
                path,
                default,
                default,
                NavigationFailureReason.None);
        }

        // 장애물로 차단된 경로 결과 생성
        public static NavigationPathResult Blocked(ObstacleHandle blockedBy, GridCell attackPosition)
        {
            // 차단 payload를 포함한 결과 반환
            return new NavigationPathResult(
                false,
                true,
                default,
                blockedBy,
                attackPosition,
                NavigationFailureReason.None);
        }

        // 실패한 경로 결과 생성
        public static NavigationPathResult Failed(NavigationFailureReason failureReason)
        {
            // 실패 사유를 포함한 결과 반환
            return new NavigationPathResult(
                false,
                false,
                default,
                default,
                default,
                failureReason);
        }
    }
}
