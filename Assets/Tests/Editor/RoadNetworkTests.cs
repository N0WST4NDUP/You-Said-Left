using System;
using NUnit.Framework;
using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class RoadNetworkTests
    {
        [Test]
        public void 같은_수평_좌표의_다른_높이_도로_노드를_각각_조회한다()
        {
            var network = new RoadNetwork();

            network.AddNode(new RoadNode(
                id: 17, coordinates: new Vector2Int(2, 5), level: 1));
            network.AddNode(new RoadNode(
                id: 42, coordinates: new Vector2Int(2, 5), level: 3));

            Assert.That(network.TryGetNode(17, out var lower), Is.True);
            Assert.That(network.TryGetNode(42, out var upper), Is.True);

            Assert.That(lower.Id, Is.EqualTo(17));
            Assert.That(lower.Coordinates, Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(lower.Level, Is.EqualTo(1));

            Assert.That(upper.Id, Is.EqualTo(42));
            Assert.That(upper.Coordinates, Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(upper.Level, Is.EqualTo(3));
        }

        [Test]
        public void 중복_ID를_추가하면_예외가_발생하고_기존_노드는_유지된다()
        {
            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(2, 5), 1));

            Assert.Throws<ArgumentException>(() =>
                network.AddNode(new RoadNode(17, new Vector2Int(8, 9), 3)));

            Assert.That(network.TryGetNode(17, out var original), Is.True);
            Assert.That(original.Id, Is.EqualTo(17));
            Assert.That(original.Coordinates, Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(original.Level, Is.EqualTo(1));
        }

        [Test]
        public void 조회에_실패하면_이전에_조회한_노드가_남지_않는다()
        {
            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(2, 5), 1));

            Assert.That(network.TryGetNode(17, out var node), Is.True);
            Assert.That(network.TryGetNode(999, out node), Is.False);
            Assert.That(node, Is.EqualTo(default(RoadNode)));
        }

        [Test]
        public void 명시한_두_노드만_양방향으로_직접_연결된다()
        {
            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(2, 5), 1));
            network.AddNode(new RoadNode(42, new Vector2Int(3, 5), 1));
            network.AddNode(new RoadNode(90, new Vector2Int(4, 5), 1));

            Assert.That(network.HasDirectConnection(17, 42), Is.False);
            Assert.That(network.HasDirectConnection(42, 17), Is.False);

            network.ConnectBidirectional(17, 42);

            Assert.That(network.HasDirectConnection(17, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 17), Is.True);
            Assert.That(network.HasDirectConnection(42, 90), Is.False);
            Assert.That(network.HasDirectConnection(90, 42), Is.False);

            network.ConnectBidirectional(42, 90);

            Assert.That(network.HasDirectConnection(17, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 17), Is.True);
            Assert.That(network.HasDirectConnection(42, 90), Is.True);
            Assert.That(network.HasDirectConnection(90, 42), Is.True);
        }

        [Test]
        public void 여러_연결을_거쳐_도달하고_고립된_노드에는_도달하지_못한다()
        {
            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(2, 5), 1));
            network.AddNode(new RoadNode(42, new Vector2Int(3, 5), 1));
            network.AddNode(new RoadNode(90, new Vector2Int(4, 5), 1));
            network.AddNode(new RoadNode(120, new Vector2Int(5, 5), 1));

            network.ConnectBidirectional(17, 42);
            network.ConnectBidirectional(42, 90);

            Assert.That(network.HasDirectConnection(17, 90), Is.False);
            Assert.That(network.HasPath(17, 90), Is.True);
            Assert.That(network.HasPath(90, 17), Is.True);
            Assert.That(network.HasPath(17, 120), Is.False);
            Assert.That(network.HasPath(120, 17), Is.False);
        }

        [Test]
        public void 자기_도달은_존재하는_노드에서만_성립한다()
        {
            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(2, 5), 1));

            Assert.That(network.HasDirectConnection(17, 17), Is.False);
            Assert.That(network.HasPath(17, 17), Is.True);
            Assert.That(network.HasPath(999, 999), Is.False);
            Assert.That(network.HasPath(17, 999), Is.False);
            Assert.That(network.HasPath(999, 17), Is.False);
        }

        [Test]
        public void 다른_높이에서_교차하는_도로는_명시적_접속_없이_연결되지_않는다()
        {
            var network = new RoadNetwork();
            // 지상 노드
            network.AddNode(new RoadNode(17, new Vector2Int(1, 5), 1));
            network.AddNode(new RoadNode(42, new Vector2Int(2, 5), 1));
            network.AddNode(new RoadNode(90, new Vector2Int(3, 5), 1));
            // 고가 노드
            network.AddNode(new RoadNode(120, new Vector2Int(2, 4), 3));
            network.AddNode(new RoadNode(150, new Vector2Int(2, 5), 3));
            network.AddNode(new RoadNode(180, new Vector2Int(2, 6), 3));

            network.ConnectBidirectional(17, 42);
            network.ConnectBidirectional(42, 90);

            network.ConnectBidirectional(120, 150);
            network.ConnectBidirectional(150, 180);

            Assert.That(network.HasPath(17, 90), Is.True);
            Assert.That(network.HasPath(90, 17), Is.True);
            Assert.That(network.HasPath(120, 180), Is.True);
            Assert.That(network.HasPath(180, 120), Is.True);
            Assert.That(network.HasPath(42, 150), Is.False);
            Assert.That(network.HasPath(150, 42), Is.False);
            Assert.That(network.HasPath(17, 180), Is.False);
            Assert.That(network.HasPath(180, 17), Is.False);
            Assert.That(network.HasDirectConnection(42, 150), Is.False);
            Assert.That(network.HasDirectConnection(150, 42), Is.False);
        }

        [Test]
        public void 램프용_중간_구간의_양끝을_연결해야_두_도로_사이에_도달한다()
        {
            var network = new RoadNetwork();
            // 지상 노드
            network.AddNode(new RoadNode(17, new Vector2Int(1, 5), 1));
            network.AddNode(new RoadNode(42, new Vector2Int(2, 5), 1));
            network.AddNode(new RoadNode(90, new Vector2Int(3, 5), 1));
            // 고가 노드
            network.AddNode(new RoadNode(120, new Vector2Int(2, 4), 3));
            network.AddNode(new RoadNode(150, new Vector2Int(2, 5), 3));
            network.AddNode(new RoadNode(180, new Vector2Int(2, 6), 3));

            // 지상 연결
            network.ConnectBidirectional(17, 42);
            network.ConnectBidirectional(42, 90);
            // 고가 연결
            network.ConnectBidirectional(120, 150);
            network.ConnectBidirectional(150, 180);

            // 램프용 중간 노드
            network.AddNode(new RoadNode(200, new Vector2Int(3, 6), 2));

            // 중간 노드 연결 전
            Assert.That(network.HasPath(17, 180), Is.False);
            Assert.That(network.HasPath(180, 17), Is.False);

            // 90 ↔ 200만 연결
            network.ConnectBidirectional(90, 200);
            Assert.That(network.HasPath(17, 180), Is.False);
            Assert.That(network.HasPath(180, 17), Is.False);

            // 200 ↔ 180까지 연결
            network.ConnectBidirectional(200, 180);
            Assert.That(network.HasPath(17, 180), Is.True);
            Assert.That(network.HasPath(180, 17), Is.True);

            // 추가 검증
            Assert.That(network.HasPath(42, 150), Is.True);
            Assert.That(network.HasPath(150, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 150), Is.False);
            Assert.That(network.HasDirectConnection(150, 42), Is.False);
            Assert.That(network.HasPath(17, 90), Is.True);
            Assert.That(network.HasPath(90, 17), Is.True);
            Assert.That(network.HasPath(120, 180), Is.True);
            Assert.That(network.HasPath(180, 120), Is.True);
        }
    }
}
