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

        [Test]
        public void 터널의_입구와_출구는_내부를_통해_이어지고_지표_도로와_분리된다()
        {
            var network = new RoadNetwork();
            // 입구 앞 도로
            network.AddNode(new RoadNode(10, new Vector2Int(0, 5), 1));
            // 터널 입구
            network.AddNode(new RoadNode(17, new Vector2Int(1, 5), 1));
            // 터널 내부
            network.AddNode(new RoadNode(42, new Vector2Int(2, 5), 1));
            // 터널 출구
            network.AddNode(new RoadNode(90, new Vector2Int(3, 5), 1));
            // 출구 뒤 도로
            network.AddNode(new RoadNode(100, new Vector2Int(4, 5), 1));
            // 지표 도로 시작
            network.AddNode(new RoadNode(120, new Vector2Int(2, 4), 3));
            // 지표 도로 중앙
            network.AddNode(new RoadNode(150, new Vector2Int(2, 5), 3));
            // 지표 도로 끝
            network.AddNode(new RoadNode(180, new Vector2Int(2, 6), 3));

            // 초기 연결
            network.ConnectBidirectional(10, 17);
            network.ConnectBidirectional(90, 100);
            network.ConnectBidirectional(120, 150);
            network.ConnectBidirectional(150, 180);

            // 터널 연결 확인
            Assert.That(network.HasPath(10, 100), Is.False);
            Assert.That(network.HasPath(100, 10), Is.False);

            // 입구 - 중간 연결 후 확인
            network.ConnectBidirectional(17, 42);
            Assert.That(network.HasPath(10, 100), Is.False);
            Assert.That(network.HasPath(100, 10), Is.False);

            // 중간 - 출구 연결 후 확인
            network.ConnectBidirectional(42, 90);
            Assert.That(network.HasPath(10, 100), Is.True);
            Assert.That(network.HasPath(100, 10), Is.True);

            // 내부를 경유해야 하는지 확인
            Assert.That(network.HasDirectConnection(17, 90), Is.False);
            Assert.That(network.HasDirectConnection(90, 17), Is.False);
            Assert.That(network.HasDirectConnection(42, 150), Is.False);
            Assert.That(network.HasDirectConnection(150, 42), Is.False);

            // 터널과 지표 연결 확인
            Assert.That(network.HasPath(42, 150), Is.False);
            Assert.That(network.HasPath(150, 42), Is.False);

            // 지표 경로 유지 확인
            Assert.That(network.HasPath(120, 180), Is.True);
            Assert.That(network.HasPath(180, 120), Is.True);
        }

        [Test]
        public void 물_위의_도로를_연결해도_지형_바닥과_수면은_유지된다()
        {
            var generator = new BlueprintGenerator((x, y) => x == 1 ? 0 : 3);
            var blueprint = generator.CreateBlueprint(width: 3, height: 1, baseLevel: 0, waterLevel: 2);

            var expectedLevels = new[] { 3, 0, 3 };
            var expectedKinds = new[]
            {
                TerrainKind.Grass,
                TerrainKind.Water,
                TerrainKind.Grass,
            };

            void AssertTerrain()
            {
                for (var x = 0; x < expectedLevels.Length; x++)
                {
                    Assert.That(blueprint.TryGetCell(x, 0, out var cell), Is.True);
                    Assert.That(cell.Level, Is.EqualTo(expectedLevels[x]));
                    Assert.That(cell.Kind, Is.EqualTo(expectedKinds[x]));
                }

                Assert.That(blueprint.WaterLevel, Is.EqualTo(2));
            }

            AssertTerrain();

            var network = new RoadNetwork();
            network.AddNode(new RoadNode(17, new Vector2Int(0, 0), 3));
            network.AddNode(new RoadNode(42, new Vector2Int(1, 0), 3));
            network.AddNode(new RoadNode(90, new Vector2Int(2, 0), 3));

            network.ConnectBidirectional(17, 42);
            network.ConnectBidirectional(42, 90);

            Assert.That(network.HasPath(17, 90), Is.True);
            Assert.That(network.HasPath(90, 17), Is.True);
            Assert.That(network.HasDirectConnection(17, 90), Is.False);
            Assert.That(network.HasDirectConnection(90, 17), Is.False);

            Assert.That(network.TryGetNode(42, out var middleRoad), Is.True);
            Assert.That(middleRoad.Coordinates, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(middleRoad.Level, Is.EqualTo(3));

            AssertTerrain();
        }

        [Test]
        public void 첫_번째_노드가_없으면_예외가_발생하고_기존_연결은_유지된다()
        {
            var network = new RoadNetwork();
            var firstNode = new RoadNode(17, new Vector2Int(2, 5), 1);
            var secondNode = new RoadNode(42, new Vector2Int(3, 5), 1);

            network.AddNode(firstNode);
            network.AddNode(secondNode);
            network.ConnectBidirectional(17, 42);

            Assert.Throws<ArgumentException>(() =>
                network.ConnectBidirectional(999, 17));

            // 기존 양방향 연결이 유지된다.
            Assert.That(network.HasDirectConnection(17, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 17), Is.True);

            // 실패한 요청의 연결이 어느 방향에도 남지 않는다.
            Assert.That(network.HasDirectConnection(999, 17), Is.False);
            Assert.That(network.HasDirectConnection(17, 999), Is.False);

            // 없는 노드가 추가되지 않고, 기존 노드 값도 유지된다.
            Assert.That(network.TryGetNode(999, out _), Is.False);

            Assert.That(network.TryGetNode(17, out var actualFirst), Is.True);
            Assert.That(actualFirst, Is.EqualTo(firstNode));

            Assert.That(network.TryGetNode(42, out var actualSecond), Is.True);
            Assert.That(actualSecond, Is.EqualTo(secondNode));
        }

        [Test]
        public void 두_번째_노드가_없으면_예외가_발생하고_기존_연결은_유지된다()
        {
            var network = new RoadNetwork();
            var firstNode = new RoadNode(17, new Vector2Int(2, 5), 1);
            var secondNode = new RoadNode(42, new Vector2Int(3, 5), 1);

            network.AddNode(firstNode);
            network.AddNode(secondNode);
            network.ConnectBidirectional(17, 42);

            Assert.Throws<ArgumentException>(() =>
                network.ConnectBidirectional(17, 999));

            // 기존 양방향 연결이 유지된다.
            Assert.That(network.HasDirectConnection(17, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 17), Is.True);

            // 실패한 요청의 연결이 어느 방향에도 남지 않는다.
            Assert.That(network.HasDirectConnection(17, 999), Is.False);
            Assert.That(network.HasDirectConnection(999, 17), Is.False);

            // 없는 노드가 추가되지 않고, 기존 노드 값도 유지된다.
            Assert.That(network.TryGetNode(999, out _), Is.False);

            Assert.That(network.TryGetNode(17, out var actualFirst), Is.True);
            Assert.That(actualFirst, Is.EqualTo(firstNode));

            Assert.That(network.TryGetNode(42, out var actualSecond), Is.True);
            Assert.That(actualSecond, Is.EqualTo(secondNode));
        }

        [Test]
        public void 양쪽_노드가_없으면_예외가_발생하고_기존_연결은_유지된다()
        {
            var network = new RoadNetwork();
            var firstNode = new RoadNode(17, new Vector2Int(2, 5), 1);
            var secondNode = new RoadNode(42, new Vector2Int(3, 5), 1);

            network.AddNode(firstNode);
            network.AddNode(secondNode);
            network.ConnectBidirectional(17, 42);

            Assert.Throws<ArgumentException>(() =>
                network.ConnectBidirectional(998, 999));

            // 기존 양방향 연결이 유지된다.
            Assert.That(network.HasDirectConnection(17, 42), Is.True);
            Assert.That(network.HasDirectConnection(42, 17), Is.True);

            // 실패한 요청의 연결이 어느 방향에도 남지 않는다.
            Assert.That(network.HasDirectConnection(998, 999), Is.False);
            Assert.That(network.HasDirectConnection(999, 998), Is.False);

            // 없는 두 노드가 추가되지 않는다.
            Assert.That(network.TryGetNode(998, out _), Is.False);
            Assert.That(network.TryGetNode(999, out _), Is.False);

            // 기존 노드 값도 유지된다.
            Assert.That(network.TryGetNode(17, out var actualFirst), Is.True);
            Assert.That(actualFirst, Is.EqualTo(firstNode));

            Assert.That(network.TryGetNode(42, out var actualSecond), Is.True);
            Assert.That(actualSecond, Is.EqualTo(secondNode));
        }
    }
}
