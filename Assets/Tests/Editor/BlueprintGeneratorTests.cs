using NUnit.Framework;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class BlueprintTests
    {
        [Test]
        public void 초기_청사진의_모든_좌표에서_지형_셀을_조회할_수_있다()
        {
            var blueprint = new Blueprint(width: 3, height: 2);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    Assert.That(
                        blueprint.TryGetCell(x, y, out _),
                        Is.True,
                        $"({x}, {y})의 지형 셀을 조회하지 못했습니다."
                    );
                }
            }
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void 격자_범위_밖의_좌표는_조회할_수_없다(int x, int y)
        {
            var blueprint = new Blueprint(width: 3, height: 2);

            var found = blueprint.TryGetCell(x, y, out _);

            Assert.That(found, Is.False);
        }

        [Test]
        public void 초기_청사진의_모든_지형_셀은_풀밭이다()
        {
            var blueprint = new Blueprint(width: 3, height: 2);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Kind,
                        Is.EqualTo(TerrainKind.Grass),
                        $"({x}, {y})의 초기 지형이 Grass가 아닙니다."
                    );
                }
            }
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void 초기_청사진의_모든_셀에_지정한_높이_층이_적용된다(int baseLevel)
        {
            var blueprint = new Blueprint(
                width: 3,
                height: 2,
                baseLevel: baseLevel
            );

            Assert.That(blueprint.BaseLevel, Is.EqualTo(baseLevel));

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Level,
                        Is.EqualTo(baseLevel),
                        $"({x}, {y})에 초기 높이 층이 적용되지 않았습니다."
                    );
                }
            }
        }

        [Test]
        public void 지형_셀_변경은_지정한_좌표에만_적용된다()
        {
            var blueprint = new Blueprint(
                width: 3,
                height: 2,
                baseLevel: 2
            );

            var changed = blueprint.TrySetCell(
                x: 2,
                y: 1,
                cell: new TerrainCell(level: 3)
            );

            Assert.That(changed, Is.True);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);
                    var expectedLevel = x == 2 && y == 1 ? 3 : 2;

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Level,
                        Is.EqualTo(expectedLevel),
                        $"({x}, {y})의 높이 층이 예상과 다릅니다."
                    );
                }
            }
        }

        [TestCase(-1, 0)]
        [TestCase(3, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 2)]
        public void 범위_밖의_셀_변경은_기존_데이터를_유지한다(int invalidX, int invalidY)
        {
            var blueprint = new Blueprint(
                width: 3,
                height: 2,
                baseLevel: 2
            );

            var changed = blueprint.TrySetCell(
                invalidX,
                invalidY,
                new TerrainCell(level: 3)
            );

            Assert.That(changed, Is.False);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Level,
                        Is.EqualTo(2),
                        $"실패한 변경이 ({x}, {y})의 높이를 바꿨습니다."
                    );
                    Assert.That(cell.Kind, Is.EqualTo(TerrainKind.Grass));
                }
            }
        }

        [Test]
        public void 지정한_셀만_물로_변경되고_높이는_유지된다()
        {
            var blueprint = new Blueprint(
                width: 3,
                height: 2,
                baseLevel: 2
            );

            var changed = blueprint.TrySetCell(
                x: 2,
                y: 1,
                cell: new TerrainCell(level: 2, kind: TerrainKind.Water)
            );

            Assert.That(changed, Is.True);

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);
                    var expectedKind = x == 2 && y == 1
                        ? TerrainKind.Water
                        : TerrainKind.Grass;

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Kind,
                        Is.EqualTo(expectedKind),
                        $"({x}, {y})의 지형 종류가 예상과 다릅니다."
                    );
                    Assert.That(cell.Level, Is.EqualTo(2));
                }
            }
        }

    }

    public class BlueprintGeneratorTests
    {
        [Test]
        public void 생성된_청사진에_크기가_유지된다()
        {
            var generator = new BlueprintGenerator();
            var blueprint = generator.CreateBlueprint(
                width: 3,
                height: 2
            );

            Assert.That(blueprint.Width, Is.EqualTo(3));
            Assert.That(blueprint.Height, Is.EqualTo(2));
        }
    }
}
