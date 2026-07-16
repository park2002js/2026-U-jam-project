using System;
using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public readonly struct NavigationPathRequest
    {
        // 경로 계산 시작 Cell 저장
        public NavigationPathRequest(
            GridCell currentCell,
            NavigationRequest navigationRequest,
            IGridNavigation gridNavigation,
            ITraversalCapability traversalCapability)
        {
            // Grid Navigation 계약 필수 여부 확인
            if (gridNavigation == null)
            {
                throw new ArgumentNullException(nameof(gridNavigation));
            }

            // Traversal 계약 필수 여부 확인
            if (traversalCapability == null)
            {
                throw new ArgumentNullException(nameof(traversalCapability));
            }

            CurrentCell = currentCell;
            Request = navigationRequest;
            GridNavigation = gridNavigation;
            TraversalCapability = traversalCapability;
        }

        // 경로 계산 시작 Cell
        public GridCell CurrentCell { get; }

        // 원본 이동 요청
        public NavigationRequest Request { get; }

        // 통과 가능성과 비용을 조회할 Grid 계약
        public IGridNavigation GridNavigation { get; }

        // 통과 능력을 평가할 Provider 계약
        public ITraversalCapability TraversalCapability { get; }
    }
}
