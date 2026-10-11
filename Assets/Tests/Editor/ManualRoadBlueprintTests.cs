using NUnit.Framework;
using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class ManualRoadBlueprintTests
    {
        [Test]
        public void 청사진의_도로_구성은_기존_지형과_수면을_수정하지_않는다()
        {
            var blueprint = new Blueprint(3, 3, 1, 2);
            blueprint.TrySetCell(1, 1, new TerrainCell(0, TerrainKind.Water));
            blueprint.Roads.AddModule(1, new RoadModulePlacement(new Vector2Int(1, 1), 3),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            Assert.That(blueprint.WaterLevel, Is.EqualTo(2));
            Assert.That(blueprint.TryGetCell(1, 1, out var cell), Is.True);
            Assert.That(cell.Level, Is.EqualTo(0));
            Assert.That(cell.Kind, Is.EqualTo(TerrainKind.Water));
            Assert.That(blueprint.TryGetCell(0, 0, out var grass), Is.True);
            Assert.That(grass.Level, Is.EqualTo(1));
            Assert.That(grass.Kind, Is.EqualTo(TerrainKind.Grass));
        }

        [Test]
        public void 수동_램프의_양끝_높이는_각_이웃과_맞고_명시한_경로로_도달한다()
        {
            var roads = ManualRoadBlueprint.Create().Roads;
            var lower = Find(roads.GetModules(), 3);
            var ramp = Find(roads.GetModules(), 4);
            var upper = Find(roads.GetModules(), 5);
            Assert.That(ramp.GetPort(0).Port.BoundaryLevel, Is.EqualTo(1));
            Assert.That(ramp.GetPort(1).Port.BoundaryLevel, Is.EqualTo(2));
            Assert.That(ramp.GetPort(0).ContactPoint, Is.EqualTo(lower.GetPort(1).ContactPoint));
            Assert.That(ramp.GetPort(1).ContactPoint, Is.EqualTo(upper.GetPort(0).ContactPoint));
            Assert.That(roads.HasPath(lower.GetNodeId(0), upper.GetNodeId(1)), Is.True);
            Assert.That(roads.HasPath(upper.GetNodeId(1), lower.GetNodeId(0)), Is.True);
        }

        [Test]
        public void 수동_입체교차는_각_도로의_내부_경로만_존재하고_층_사이는_연결되지_않는다()
        {
            var roads = ManualRoadBlueprint.Create().Roads;
            var lower = Find(roads.GetModules(), 6);
            var upper = Find(roads.GetModules(), 7);
            Assert.That(lower.Placement.CellCoordinates, Is.EqualTo(upper.Placement.CellCoordinates));
            Assert.That(lower.GetPort(0).Port.BoundaryLevel, Is.EqualTo(1));
            Assert.That(upper.GetPort(0).Port.BoundaryLevel, Is.EqualTo(3));
            Assert.That(roads.HasPath(lower.GetNodeId(0), lower.GetNodeId(1)), Is.True);
            Assert.That(roads.HasPath(upper.GetNodeId(0), upper.GetNodeId(1)), Is.True);
            for (var i = 0; i < 2; i++)
            for (var j = 0; j < 2; j++)
            {
                Assert.That(roads.HasPath(lower.GetNodeId(i), upper.GetNodeId(j)), Is.False);
                Assert.That(roads.HasPath(upper.GetNodeId(j), lower.GetNodeId(i)), Is.False);
            }
        }

        [Test]
        public void 수동_청사진의_인접한_평지_직선은_경계_접속을_통해_도달한다()
        {
            var blueprint = ManualRoadBlueprint.Create();
            var roads = blueprint.Roads;
            var modules = roads.GetModules();
            var first = Find(modules, 1);
            var second = Find(modules, 2);
            Assert.That(roads.HasPath(first.GetNodeId(0), second.GetNodeId(1)), Is.True);
            Assert.That(roads.HasPath(second.GetNodeId(1), first.GetNodeId(0)), Is.True);
            Assert.That(first.GetPort(1).ContactPoint, Is.EqualTo(second.GetPort(0).ContactPoint));
            Assert.That(blueprint.TryGetCell(1, 1, out var cell), Is.True);
            Assert.That(cell.Level, Is.EqualTo(0));
            Assert.That(cell.Kind, Is.EqualTo(TerrainKind.Grass));
        }

        private static PlacedRoadModule Find(System.Collections.Generic.IReadOnlyList<PlacedRoadModule> modules, int id)
        {
            foreach (var module in modules)
                if (module.Id == id) return module;
            Assert.Fail($"모듈 {id}가 없다.");
            return null;
        }
    }
}
