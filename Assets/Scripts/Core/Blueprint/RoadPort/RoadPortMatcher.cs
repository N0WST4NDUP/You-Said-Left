using UnityEngine;

namespace YouSaidLeft.Core
{
    public static class RoadPortMatcher
    {
        public static bool MatchesLogicalBoundary(RoadPort first, RoadPort second)
        {
            if (first.BoundaryLevel != second.BoundaryLevel)
            {
                return false;
            }

            return (first.Side, second.Side) switch
            {
                (RoadPortSide.PositiveX, RoadPortSide.NegativeX)
                    => second.CellCoordinates == first.CellCoordinates + new Vector2Int(1, 0),
                (RoadPortSide.NegativeX, RoadPortSide.PositiveX)
                    => second.CellCoordinates == first.CellCoordinates + new Vector2Int(-1, 0),
                (RoadPortSide.PositiveY, RoadPortSide.NegativeY)
                    => second.CellCoordinates == first.CellCoordinates + new Vector2Int(0, 1),
                (RoadPortSide.NegativeY, RoadPortSide.PositiveY)
                    => second.CellCoordinates == first.CellCoordinates + new Vector2Int(0, -1),
                _ => false,
            };
        }
    }
}
