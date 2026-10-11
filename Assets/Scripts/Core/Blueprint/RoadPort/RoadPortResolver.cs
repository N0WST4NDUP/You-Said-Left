using System;
using UnityEngine;

namespace YouSaidLeft.Core
{
    public readonly struct ResolvedRoadPort
    {
        public readonly RoadPort Port;
        public readonly Vector2 ContactPoint;

        public ResolvedRoadPort(RoadPort port, Vector2 contactPoint)
        {
            Port = port;
            ContactPoint = contactPoint;
        }
    }

    public static class RoadPortResolver
    {
        public static ResolvedRoadPort ResolveUnrotated(RoadModulePlacement placement, RoadPortDefinition definition)
        {
            ValidateDefinition(in definition);

            var roadPort = new RoadPort(
                placement.CellCoordinates,
                definition.Side,
                checked(placement.BaseLevel + definition.LevelOffset));
            return new(
                roadPort,
                placement.CellCoordinates + definition.LocalPosition
            );
        }

        private static void ValidateDefinition(in RoadPortDefinition definition)
        {
            var position = definition.LocalPosition;

            if (float.IsNaN(position.x) || float.IsNaN(position.y))
            {
                throw new ArgumentException(
                    "위치 정보는 NaN일 수 없습니다.",
                    nameof(definition));
            }

            if (!float.IsFinite(position.x) || !float.IsFinite(position.y))
            {
                throw new ArgumentException(
                    "로컬 위치는 유한한 값이어야 합니다.",
                    nameof(definition));
            }

            if (!Enum.IsDefined(typeof(RoadPortSide), definition.Side))
            {
                throw new ArgumentException(
                    "정의되지 않은 도로 포트 방향입니다.",
                    nameof(definition));
            }
        }
    }
}
