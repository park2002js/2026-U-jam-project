namespace UJam.Runtime.Grid
{
    public interface IGridOccupancy
    {
        bool TryOccupy(GridCell origin, GridFootprint footprint, out long handle);

        bool TryRelease(long handle);

        bool IsOccupied(GridCell cell);
    }
}
