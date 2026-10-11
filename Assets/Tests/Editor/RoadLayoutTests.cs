using System;
using NUnit.Framework;
using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class RoadLayoutTests
    {
        [TestCase(999, 0)]
        [TestCase(7, -1)]
        [TestCase(7, 2)]
        public void 없는_모듈과_포트_인덱스는_연결을_부분_추가하지_않는다(int moduleId, int portIndex)
        {
            var layout = new RoadLayout(5, 4);
            var first = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            layout.AddModule(8, new RoadModulePlacement(new Vector2Int(3, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0));
            Assert.That(() => layout.ConnectInternal(moduleId, 0, portIndex), Throws.InstanceOf<ArgumentException>());
            Assert.That(() => layout.TryConnectExternal(8, 0, moduleId, portIndex), Throws.InstanceOf<ArgumentException>());
            Assert.That(layout.GetConnections(), Is.Empty);
            Assert.That(layout.HasPath(first.GetNodeId(0), first.GetNodeId(1)), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 나중_포트의_해석_실패는_모듈과_노드를_부분_등록하지_않는다(bool overflow)
        {
            var layout = new RoadLayout(5, 4);
            layout.AddModule(7, new RoadModulePlacement(new Vector2Int(1, 1), 3),
                new RoadPortDefinition(Vector2.zero, RoadPortSide.PositiveX, 0));
            var before = layout.GetNodes();
            var definitions = new[]
            {
                new RoadPortDefinition(Vector2.zero, RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(overflow ? Vector2.right : new Vector2(float.NaN, 0),
                    RoadPortSide.PositiveX, overflow ? int.MaxValue : 0)
            };
            if (overflow)
                Assert.Throws<OverflowException>(() => layout.AddModule(8,
                    new RoadModulePlacement(new Vector2Int(2, 1), 3), definitions));
            else
                Assert.Throws<ArgumentException>(() => layout.AddModule(8,
                    new RoadModulePlacement(new Vector2Int(2, 1), 3), definitions));
            Assert.That(layout.GetModules().Count, Is.EqualTo(1));
            CollectionAssert.AreEqual(before, layout.GetNodes());
            Assert.That(layout.GetConnections(), Is.Empty);
            var next = layout.AddModule(8, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(Vector2.zero, RoadPortSide.NegativeX, 0));
            Assert.That(next.GetNodeId(0), Is.EqualTo(before[0].Id + 1));
        }

        [TestCase(0, RoadPortSide.NegativeX, 1, 0f)]
        [TestCase(0, RoadPortSide.PositiveX, 0, 0f)]
        [TestCase(1, RoadPortSide.NegativeX, 0, 0f)]
        [TestCase(0, RoadPortSide.NegativeX, 0, 0.25f)]
        [TestCase(0, RoadPortSide.NegativeX, 0, 0.000001f)]
        public void 호환하지_않는_외부_접속은_기존_경로와_간선을_보존한다(
            int cellOffset, RoadPortSide side, int levelOffset, float pointOffset)
        {
            var layout = new RoadLayout(6, 4);
            var first = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            var second = layout.AddModule(8, new RoadModulePlacement(new Vector2Int(3 + cellOffset, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, pointOffset), side, levelOffset),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            layout.ConnectInternal(7, 0, 1);
            layout.ConnectInternal(8, 0, 1);
            var before = layout.GetConnections();
            Assert.That(layout.TryConnectExternal(7, 1, 8, 0), Is.False);
            Assert.That(layout.TryConnectExternal(8, 0, 7, 1), Is.False);
            CollectionAssert.AreEqual(before, layout.GetConnections());
            Assert.That(layout.HasPath(first.GetNodeId(0), first.GetNodeId(1)), Is.True);
            Assert.That(layout.HasPath(second.GetNodeId(0), second.GetNodeId(1)), Is.True);
            Assert.That(layout.HasPath(first.GetNodeId(0), second.GetNodeId(1)), Is.False);
        }

        [Test]
        public void 모듈과_간선_스냅샷은_외부_수정과_나중_등록의_영향을_받지_않는다()
        {
            var layout = new RoadLayout(5, 4);
            var module = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 1));
            layout.ConnectInternal(7, 0, 1);
            layout.ConnectInternal(7, 1, 0);
            var modules = layout.GetModules();
            var connections = layout.GetConnections();
            Assert.That(connections.Count, Is.EqualTo(1), "양방향 간선은 표시 스냅샷에 한 번만 나타난다.");
            Assert.That(connections[0].FirstNodeId, Is.EqualTo(module.GetNodeId(0)));
            Assert.That(connections[0].SecondNodeId, Is.EqualTo(module.GetNodeId(1)));
            Assert.That(connections[0].IsExternal, Is.False);
            layout.AddModule(8, new RoadModulePlacement(new Vector2Int(3, 1), 4),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0));
            layout.TryConnectExternal(7, 1, 8, 0);
            Assert.That(modules.Count, Is.EqualTo(1));
            Assert.That(connections.Count, Is.EqualTo(1));
            if (modules is PlacedRoadModule[] moduleArray) moduleArray[0] = null;
            else if (modules is System.Collections.Generic.IList<PlacedRoadModule> mutableModules && !mutableModules.IsReadOnly)
                mutableModules[0] = null;
            if (connections is RoadConnection[] connectionArray) connectionArray[0] = default;
            else if (connections is System.Collections.Generic.IList<RoadConnection> mutableEdges && !mutableEdges.IsReadOnly)
                mutableEdges[0] = default;
            var current = layout.GetModules();
            var currentEdges = layout.GetConnections();
            Assert.That(current.Count, Is.EqualTo(2));
            CollectionAssert.Contains(current, module);
            Assert.That(currentEdges.Count, Is.EqualTo(2));
            Assert.That(currentEdges[0].SecondNodeId, Is.EqualTo(module.GetNodeId(1)));
            Assert.That(currentEdges[1].IsExternal, Is.True);
        }

        [Test]
        public void 호환_경계의_노드_ID를_병합하지_않고_명시적으로_외부_연결한다()
        {
            var layout = new RoadLayout(5, 4);
            var first = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 0));
            var second = layout.AddModule(8, new RoadModulePlacement(new Vector2Int(3, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0));
            Assert.That(first.GetNodeId(0), Is.Not.EqualTo(second.GetNodeId(0)));
            Assert.That(first.GetPort(0).ContactPoint, Is.EqualTo(second.GetPort(0).ContactPoint));
            Assert.That(layout.HasPath(first.GetNodeId(0), second.GetNodeId(0)), Is.False);
            Assert.That(layout.TryConnectExternal(7, 0, 8, 0), Is.True);
            Assert.That(layout.HasPath(first.GetNodeId(0), second.GetNodeId(0)), Is.True);
            Assert.That(layout.HasPath(second.GetNodeId(0), first.GetNodeId(0)), Is.True);
            Assert.That(layout.GetNodes().Count, Is.EqualTo(2));
        }

        [Test]
        public void 내부_간선을_명시하기_전에는_같은_모듈의_포트도_연결되지_않는다()
        {
            var layout = new RoadLayout(5, 4);
            var module = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 1));
            Assert.That(layout.HasPath(module.GetNodeId(0), module.GetNodeId(1)), Is.False);
            layout.ConnectInternal(7, 0, 1);
            Assert.That(layout.HasPath(module.GetNodeId(0), module.GetNodeId(1)), Is.True);
            Assert.That(layout.HasPath(module.GetNodeId(1), module.GetNodeId(0)), Is.True);
        }

        [Test]
        public void 배치의_포트에서_서로_다른_경계_노드의_좌표와_높이를_유도한다()
        {
            var layout = new RoadLayout(5, 4);
            var definitions = new[]
            {
                new RoadPortDefinition(new Vector2(-0.5f, 0), RoadPortSide.NegativeX, 0),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 1)
            };
            var module = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3), definitions);
            var first = module.GetPort(0);
            var second = module.GetPort(1);
            Assert.That(first.ContactPoint, Is.EqualTo(new Vector2(1.5f, 1)));
            Assert.That(second.ContactPoint, Is.EqualTo(new Vector2(2.5f, 1)));
            Assert.That(first.Port.BoundaryLevel, Is.EqualTo(3));
            Assert.That(second.Port.BoundaryLevel, Is.EqualTo(4));
            Assert.That(module.GetNodeId(0), Is.Not.EqualTo(module.GetNodeId(1)));
            definitions[0] = new RoadPortDefinition(Vector2.zero, RoadPortSide.PositiveY, 9);
            Assert.That(module.GetDefinition(0).LocalPosition, Is.EqualTo(new Vector2(-0.5f, 0)));
            Assert.That(module.GetPort(0).ContactPoint, Is.EqualTo(new Vector2(1.5f, 1)));
        }

        [Test]
        public void 노드_조회는_포트에서_유도한_값의_독립_스냅샷이다()
        {
            var layout = new RoadLayout(5, 4);
            var module = layout.AddModule(7, new RoadModulePlacement(new Vector2Int(2, 1), 3),
                new RoadPortDefinition(new Vector2(0.5f, 0), RoadPortSide.PositiveX, 1));
            var snapshot = layout.GetNodes();
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot[0].Id, Is.EqualTo(module.GetNodeId(0)));
            Assert.That(snapshot[0].Coordinates, Is.EqualTo(new Vector2(2.5f, 1)));
            Assert.That(snapshot[0].Level, Is.EqualTo(4));
            if (snapshot is RoadNode[] nodeArray) nodeArray[0] = default;
            else if (snapshot is System.Collections.Generic.IList<RoadNode> mutable && !mutable.IsReadOnly)
            {
                mutable[0] = default;
            }
            var current = layout.GetNodes();
            Assert.That(current[0].Level, Is.EqualTo(4));
        }
    }
}
