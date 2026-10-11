using UnityEngine;

namespace YouSaidLeft.Core
{
    public enum RoadPortSide
    {
        PositiveX,
        NegativeX,
        PositiveY,
        NegativeY,
    }

    public readonly struct RoadPort
    {
        public readonly Vector2Int CellCoordinates;
        public readonly RoadPortSide Side;
        public readonly int BoundaryLevel;

        public RoadPort(Vector2Int cellCoordinates, RoadPortSide side, int boundaryLevel)
        {
            CellCoordinates = cellCoordinates;
            Side = side;
            BoundaryLevel = boundaryLevel;
        }
    }
}
