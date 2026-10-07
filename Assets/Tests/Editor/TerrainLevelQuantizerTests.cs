using NUnit.Framework;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class TerrainLevelQuantizerTests
    {
        [TestCase(0f, 4, 0)]
        [TestCase(0.19f, 4, 0)]
        [TestCase(0.2f, 4, 1)]
        [TestCase(0.5f, 4, 2)]
        [TestCase(0.8f, 4, 4)]
        [TestCase(1f, 4, 4)]
        [TestCase(-0.01f, 4, 0)]
        [TestCase(1.01f, 4, 4)]
        [TestCase(0.8f, 2, 2)]
        [TestCase(0.7f, 2, 2)]
        public void 노이즈값을_허용된_정수_높이차로_변환한다(float noiseValue, int maxLevelOffset, int expected)
        {
            var actual = TerrainLevelQuantizer.ToLevelOffset(noiseValue, maxLevelOffset);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
