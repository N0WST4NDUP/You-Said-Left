using UnityEngine;

namespace YouSaidLeft.Core
{
    public readonly struct RoadModulePlacement
    {
        public readonly Vector2Int CellCoordinates;
        public readonly int BaseLevel;

        public RoadModulePlacement(Vector2Int cellCoordinates, int baseLevel)
        {
            CellCoordinates = cellCoordinates;
            BaseLevel = baseLevel;
        }
    }
}
