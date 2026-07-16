namespace UJam.Runtime.Grid
{
    public interface IGridNavigation
    {
        bool IsPassable(GridCell cell);

        float GetMovementCost(GridCell cell);
    }
}
