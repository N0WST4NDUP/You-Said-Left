using UnityEngine;

namespace YouSaidLeft.Core
{
    public readonly struct RoadPortDefinition
    {
        public readonly Vector2 LocalPosition;
        public readonly RoadPortSide Side;
        public readonly int LevelOffset;

        public RoadPortDefinition(Vector2 localPosition, RoadPortSide side, int levelOffset)
        {
            LocalPosition = localPosition;
            Side = side;
            LevelOffset = levelOffset;
        }
    }
}
