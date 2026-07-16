using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct TraversalDecision
    {
        // 통과 허용 여부 저장
        private readonly bool _isAllowed;
        // 재탐색 필요 여부 저장
        private readonly bool _needsRepath;
        // 차단 장애물 Handle 저장
        private readonly ObstacleHandle _blockedBy;
        // 차단 대응 공격 위치 저장
        private readonly GridCell _attackPosition;
        // Factory로 생성된 유효한 판정인지 저장
        private readonly bool _isValid;

        // 통과 판정과 payload를 검증하고 저장
        private TraversalDecision(
            bool isAllowed,
            bool needsRepath,
            ObstacleHandle blockedBy,
            GridCell attackPosition)
        {
            // 허용과 재탐색을 동시에 반환하지 않는지 확인
            if (isAllowed && needsRepath)
            {
                throw new ArgumentException("Traversal decision cannot be allowed and repath", nameof(needsRepath));
            }

            // 차단 payload가 허용 또는 재탐색에 섞이지 않는지 확인
            if ((isAllowed || needsRepath) && blockedBy.IsValid)
            {
                throw new ArgumentException("Only blocked traversal can contain an obstacle handle", nameof(blockedBy));
            }

            // 차단 결과는 유효한 장애물 Handle을 요구
            if (!isAllowed && !needsRepath && !blockedBy.IsValid)
            {
                throw new ArgumentException("Blocked traversal requires a valid obstacle handle", nameof(blockedBy));
            }

            _isAllowed = isAllowed;
            _needsRepath = needsRepath;
            _blockedBy = blockedBy;
            _attackPosition = attackPosition;
            _isValid = true;
        }

        // Factory로 생성된 유효한 판정 여부
        public bool IsValid
        {
            get
            {
                // 기본 struct와 구분되는 판정 유효성 반환
                return _isValid;
            }
        }

        // 통과 허용 여부
        public bool IsAllowed
        {
            get
            {
                // 통과 허용 여부 반환
                return _isAllowed;
            }
        }

        // 재탐색 필요 여부
        public bool NeedsRepath
        {
            get
            {
                // 재탐색 필요 여부 반환
                return _needsRepath;
            }
        }

        // 차단 여부
        public bool IsBlocked
        {
            get
            {
                // 허용도 재탐색도 아닌 상태를 차단으로 판정
                return _isValid && !_isAllowed && !_needsRepath;
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

        // 통과 허용 판정 생성
        public static TraversalDecision Allowed()
        {
            // 허용 판정 반환
            return new TraversalDecision(true, false, default, default);
        }

        // 장애물 차단 판정 생성
        public static TraversalDecision Blocked(ObstacleHandle blockedBy, GridCell attackPosition)
        {
            // 차단 payload를 포함한 판정 반환
            return new TraversalDecision(false, false, blockedBy, attackPosition);
        }

        // 재탐색 판정 생성
        public static TraversalDecision RepathRequired()
        {
            // 재탐색 판정 반환
            return new TraversalDecision(false, true, default, default);
        }
    }
}
