using UJam.Runtime.Grid;

namespace UJam.Runtime.Navigation
{
    public interface ITraversalCapability
    {
        // 두 Cell 사이 통과 가능 여부 확인
        TraversalDecision Evaluate(GridCell from, GridCell to, TraversalProfile profile);
    }
}
