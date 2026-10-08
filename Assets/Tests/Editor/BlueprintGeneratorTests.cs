using System.Linq;
using NUnit.Framework;
using UnityEngine;
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

        [Test]
        public void 모서리에서는_범위_안의_상하좌우_이웃만_반환한다()
        {
            var blueprint = new Blueprint(width: 3, height: 2);

            var neighbors = blueprint.GetNeighborCoordinates(0, 0);

            var expected = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
            };

            Assert.That(neighbors, Is.EquivalentTo(expected));
        }

        [TestCase(3, 3, 1, 1)]
        [TestCase(3, 2, 2, 1)]
        [TestCase(1, 1, 0, 0)]
        public void 위치와_크기에_맞는_이웃_좌표를_반환한다(int width, int height, int x, int y)
        {
            var blueprint = new Blueprint(width, height);

            var expected = (width, height, x, y) switch
            {
                (3, 3, 1, 1) => new[]
                {
                    new Vector2Int(0, 1),
                    new Vector2Int(2, 1),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 2),
                },
                (3, 2, 2, 1) => new[]
                {
                    new Vector2Int(1, 1),
                    new Vector2Int(2, 0),
                },
                (1, 1, 0, 0) => System.Array.Empty<Vector2Int>(),
                _ => throw new System.InvalidOperationException(
                    "검증용 입력이 정의되지 않았습니다."
                ),
            };

            var neighbors = blueprint.GetNeighborCoordinates(x, y);

            Assert.That(neighbors, Is.EquivalentTo(expected));
        }

        [TestCase(-1, 0, "x")]
        [TestCase(3, 0, "x")]
        [TestCase(0, -1, "y")]
        [TestCase(0, 2, "y")]
        public void 범위_밖의_이웃_조회는_잘못된_좌표의_예외를_발생시킨다(int x, int y, string expectedParamName)
        {
            var blueprint = new Blueprint(width: 3, height: 2);

            var exception = Assert.Throws<System.ArgumentOutOfRangeException>(
                () => blueprint.GetNeighborCoordinates(x, y).ToArray()
            );

            Assert.That(exception.ParamName, Is.EqualTo(expectedParamName));
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

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(5)]
        public void 생성된_셀의_높이에_기준층과_높이차가_적용된다(int baseLevel)
        {
            var generator = new BlueprintGenerator(
                sampleLevelOffset: (x, y) => 2 + x + 3 * y
            );
            var blueprint = generator.CreateBlueprint(width: 3, height: 2, baseLevel: baseLevel);

            // 첫 번째 인덱스는 x, 두 번째 인덱스는 y입니다.
            var expectedLevels = baseLevel switch
            {
                0 => new int[,]
                {
                    { 2, 5 },
                    { 3, 6 },
                    { 4, 7 },
                },
                2 => new int[,]
                {
                    { 4, 7 },
                    { 5, 8 },
                    { 6, 9 },
                },
                5 => new int[,]
                {
                    { 7, 10 },
                    { 8, 11 },
                    { 9, 12 },
                },
                _ => throw new System.ArgumentOutOfRangeException(nameof(baseLevel)),
            };

            Assert.That(blueprint.BaseLevel, Is.EqualTo(baseLevel));

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Level,
                        Is.EqualTo(expectedLevels[x, y]),
                        $"({x}, {y})에 계산한 높이가 적용되지 않았습니다."
                    );
                    Assert.That(cell.Kind, Is.EqualTo(TerrainKind.Grass));
                }
            }
        }

        [Test]
        public void 생성기는_전달받은_높이차_함수를_사용한다()
        {
            var generator = new BlueprintGenerator(
                sampleLevelOffset: (x, y) => 1
            );

            var blueprint = generator.CreateBlueprint(
                width: 1,
                height: 1,
                baseLevel: 2
            );

            var found = blueprint.TryGetCell(0, 0, out var cell);

            Assert.That(found, Is.True);
            Assert.That(cell.Level, Is.EqualTo(3));
        }

        [Test]
        public void 기본_생성기는_모든_셀을_기준층_높이로_생성한다()
        {
            var generator = new BlueprintGenerator();
            var blueprint = generator.CreateBlueprint(
                width: 3,
                height: 2,
                baseLevel: 5
            );

            for (var y = 0; y < 2; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var found = blueprint.TryGetCell(x, y, out var cell);

                    Assert.That(found, Is.True);
                    Assert.That(
                        cell.Level,
                        Is.EqualTo(5),
                        $"({x}, {y})의 높이가 기준층과 다릅니다."
                    );
                }
            }
        }

        [Test]
        public void 높이차_함수가_null이면_예외가_발생한다()
        {
            var exception = Assert.Throws<System.ArgumentNullException>(
                () => new BlueprintGenerator(sampleLevelOffset: null)
            );

            Assert.That(exception.ParamName, Is.EqualTo("sampleLevelOffset"));
        }

        [Test]
        public void 수면보다_낮은_셀만_물로_생성하고_바닥_높이는_유지한다()
        {
            var generator = new BlueprintGenerator(
                sampleLevelOffset: (x, y) => x
            );
            var blueprint = generator.CreateBlueprint(
                width: 3,
                height: 1,
                baseLevel: 2,
                waterLevel: 3
            );

            var expectedLevels = new[] { 2, 3, 4 };
            var expectedKinds = new[]
            {
                TerrainKind.Water,
                TerrainKind.Grass,
                TerrainKind.Grass,
            };

            for (var x = 0; x < 3; x++)
            {
                var found = blueprint.TryGetCell(x, 0, out var cell);

                Assert.That(found, Is.True);
                Assert.That(cell.Level, Is.EqualTo(expectedLevels[x]));
                Assert.That(
                    cell.Kind,
                    Is.EqualTo(expectedKinds[x]),
                    $"({x}, 0)의 지형 종류가 예상과 다릅니다."
                );
            }

            Assert.That(blueprint.WaterLevel, Is.EqualTo(3));
        }

        [Test]
        public void 펄린_지형의_물과_육지가_같은_시드로_재현된다()
        {
            Blueprint Generate()
            {
                var sampler = PerlinLevelSampler.FromSeed(
                    seed: 12345,
                    frequency: 0.125f,
                    maxLevelOffset: 8,
                    originRange: 256f
                );
                var generator = new BlueprintGenerator(
                    sampleLevelOffset: sampler.Sample
                );

                return generator.CreateBlueprint(
                    width: 16,
                    height: 16,
                    baseLevel: 2,
                    waterLevel: 6
                );
            }

            var first = Generate();
            var second = Generate();
            var hasWater = false;
            var hasGrass = false;

            Assert.That(first.WaterLevel, Is.EqualTo(6));
            Assert.That(second.WaterLevel, Is.EqualTo(6));

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    Assert.That(first.TryGetCell(x, y, out var firstCell), Is.True);
                    Assert.That(second.TryGetCell(x, y, out var secondCell), Is.True);

                    Assert.That(
                        secondCell.Level,
                        Is.EqualTo(firstCell.Level),
                        $"({x}, {y})의 높이가 재현되지 않았습니다."
                    );
                    Assert.That(
                        secondCell.Kind,
                        Is.EqualTo(firstCell.Kind),
                        $"({x}, {y})의 지형 종류가 재현되지 않았습니다."
                    );

                    hasWater = hasWater || firstCell.Kind == TerrainKind.Water;
                    hasGrass = hasGrass || firstCell.Kind == TerrainKind.Grass;
                }
            }

            Assert.That(hasWater, Is.True, "검증용 지형에 물이 없습니다.");
            Assert.That(hasGrass, Is.True, "검증용 지형에 육지가 없습니다.");
        }
    }
}
