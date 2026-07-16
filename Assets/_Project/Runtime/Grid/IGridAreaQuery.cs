namespace UJam.Runtime.Grid
{
    public interface IGridAreaQuery
    {
        bool IsAreaWithinBounds(GridCell origin, GridFootprint footprint);

        bool IsAreaPassable(GridCell origin, GridFootprint footprint);
    }
}
