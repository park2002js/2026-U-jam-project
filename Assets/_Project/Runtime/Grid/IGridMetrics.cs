using System;
using UnityEngine;

namespace UJam.Runtime.Grid
{
    public interface IGridMetrics
    {
        float CellSize { get; }

        Vector3 Origin { get; }

        GridCell WorldToCell(Vector3 worldPosition);

        Vector3 CellToWorld(GridCell cell);

        int Version { get; }

        event Action<int> VersionChanged;
    }
}
