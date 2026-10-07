using NUnit.Framework;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class PerlinLevelSamplerTests
    {
        [TestCase(0.125f, 0, 0, 8, 4)]
        [TestCase(0.125f, 2, 0, 8, 5)]
        [TestCase(0.125f, 0, 2, 8, 3)]
        [TestCase(0.25f, 2, 0, 8, 4)]
        [TestCase(0.125f, 2, 0, 4, 2)]
        public void 좌표와_설정으로_펄린_높이차를_계산한다(float frequency, int x, int y, int maxLevelOffset, int expected)
        {
            var sampler = new PerlinLevelSampler(
                frequency: frequency,
                maxLevelOffset: maxLevelOffset
            );

            Assert.That(sampler.Sample(x, y), Is.EqualTo(expected));
        }

        [TestCase(0.25f, 0f, 0, 0, 5)]
        [TestCase(0f, 0.25f, 0, 0, 3)]
        [TestCase(0.25f, 0f, 2, 0, 4)]
        public void 샘플링_원점을_격자_좌표에_적용한다(float originX, float originY, int x, int y, int expected)
        {
            var sampler = new PerlinLevelSampler(
                frequency: 0.125f,
                maxLevelOffset: 8,
                originX: originX,
                originY: originY
            );

            Assert.That(sampler.Sample(x, y), Is.EqualTo(expected));
        }

        [Test]
        public void 선택한_두_시드로_서로_다른_지형을_생성한다()
        {
            var first = PerlinLevelSampler.FromSeed(
                seed: 12345,
                frequency: 0.125f,
                maxLevelOffset: 8,
                originRange: 256f
            );
            var second = PerlinLevelSampler.FromSeed(
                seed: 54321,
                frequency: 0.125f,
                maxLevelOffset: 8,
                originRange: 256f
            );

            var hasDifference = false;
            var hasVariation = false;
            var initialLevel = first.Sample(0, 0);

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var firstLevel = first.Sample(x, y);

                    if (firstLevel != second.Sample(x, y))
                    {
                        hasDifference = true;
                    }

                    if (firstLevel != initialLevel)
                    {
                        hasVariation = true;
                    }
                }
            }

            Assert.That(hasDifference, Is.True, "두 지형이 모두 같습니다.");
            Assert.That(hasVariation, Is.True, "첫 지형의 높이가 모두 같습니다.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 같은_시드의_지형은_조회_순서와_관계없이_재현된다(bool reverseOrder)
        {
            var first = PerlinLevelSampler.FromSeed(
                seed: 12345,
                frequency: 0.125f,
                maxLevelOffset: 8,
                originRange: 256f
            );
            var second = PerlinLevelSampler.FromSeed(
                seed: 12345,
                frequency: 0.125f,
                maxLevelOffset: 8,
                originRange: 256f
            );

            var expectedLevels = new int[16, 16];

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    expectedLevels[x, y] = first.Sample(x, y);
                }
            }

            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var queryX = reverseOrder ? 15 - x : x;
                    var queryY = reverseOrder ? 15 - y : y;

                    Assert.That(
                        second.Sample(queryX, queryY),
                        Is.EqualTo(expectedLevels[queryX, queryY]),
                        $"({queryX}, {queryY})의 높이차가 재현되지 않았습니다."
                    );
                }
            }
        }
    }
}
