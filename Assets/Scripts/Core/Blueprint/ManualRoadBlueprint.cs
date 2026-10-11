using System;
using UnityEngine;

namespace YouSaidLeft.Core
{
    // 실제 프리팹 규격이 아닌 논리 셀·높이·연결을 관찰하는 고정 사례입니다.
    public static class ManualRoadBlueprint
    {
        public static Blueprint Create()
        {
            var blueprint = new Blueprint(7, 6);
            var roads = blueprint.Roads;
            AddStraightX(roads, 1, new Vector2Int(1, 1), 1);
            AddStraightX(roads, 2, new Vector2Int(2, 1), 1);
            RequireConnection(roads, 1, 1, 2, 0);
            AddStraightX(roads, 3, new Vector2Int(1, 3), 1);
            roads.AddModule(4, new RoadModulePlacement(new Vector2Int(2, 3), 1),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 1));
            roads.ConnectInternal(4, 0, 1);
            AddStraightX(roads, 5, new Vector2Int(3, 3), 2);
            RequireConnection(roads, 3, 1, 4, 0);
            RequireConnection(roads, 4, 1, 5, 0);
            AddStraightX(roads, 6, new Vector2Int(5, 4), 1);
            roads.AddModule(7, new RoadModulePlacement(new Vector2Int(5, 4), 3),
                new RoadPortDefinition(new Vector2(0, -0.5f), RoadPortSide.NegativeY, 0),
                new RoadPortDefinition(new Vector2(0, 0.5f), RoadPortSide.PositiveY, 0));
            roads.ConnectInternal(7, 0, 1);
            return blueprint;
        }

        private static void AddStraightX(RoadLayout roads, int id, Vector2Int cell, int level)
        {
            roads.AddModule(id, new RoadModulePlacement(cell, level),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            roads.ConnectInternal(id, 0, 1);
        }

        private static void RequireConnection(RoadLayout roads, int firstId, int firstPort, int secondId, int secondPort)
        {
            if (!roads.TryConnectExternal(firstId, firstPort, secondId, secondPort))
            {
                throw new InvalidOperationException("수동 청사진의 외부 접속이 호환되지 않습니다.");
            }
        }
    }
}
