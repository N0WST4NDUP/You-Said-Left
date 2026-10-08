using NUnit.Framework;
using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class RoadPortTests
    {
        [TestCase(1, true)]
        [TestCase(2, false)]
        public void 마주보는_인접_포트는_경계높이가_같아야_맞물린다(int secondBoundaryLevel, bool expected)
        {
            var first = new RoadPort(
                new Vector2Int(2, 5),
                RoadPortSide.PositiveX,
                1
            );

            var second = new RoadPort(
                new Vector2Int(3, 5),
                RoadPortSide.NegativeX,
                secondBoundaryLevel
            );

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(first, second),
                Is.EqualTo(expected)
            );

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(second, first),
                Is.EqualTo(expected)
            );
        }


        [TestCase(RoadPortSide.NegativeX, true)]
        [TestCase(RoadPortSide.PositiveX, false)]
        [TestCase(RoadPortSide.PositiveY, false)]
        [TestCase(RoadPortSide.NegativeY, false)]
        public void 인접한_같은높이_포트는_서로_마주봐야_맞물린다(RoadPortSide secondSide, bool expected)
        {
            var first = new RoadPort(
                new Vector2Int(2, 5),
                RoadPortSide.PositiveX,
                2
            );

            var second = new RoadPort(
                new Vector2Int(3, 5),
                secondSide,
                2
            );

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(first, second),
                Is.EqualTo(expected)
            );

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(second, first),
                Is.EqualTo(expected)
            );
        }

        [TestCase(RoadPortSide.NegativeY, 2, true)]
        [TestCase(RoadPortSide.PositiveY, 2, false)]
        [TestCase(RoadPortSide.PositiveX, 2, false)]
        [TestCase(RoadPortSide.NegativeX, 2, false)]
        [TestCase(RoadPortSide.NegativeY, 3, false)]
        public void Y축_인접_포트도_높이와_반대방향이_맞아야_맞물린다(RoadPortSide secondSide, int secondBoundaryLevel, bool expected)
        {
            var first = new RoadPort(
                new Vector2Int(2, 5),
                RoadPortSide.PositiveY,
                2);

            var second = new RoadPort(
                new Vector2Int(2, 6),
                secondSide,
                secondBoundaryLevel);

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(first, second),
                Is.EqualTo(expected));

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(second, first),
                Is.EqualTo(expected));
        }

        [TestCase(3, 5, true)]   // 바라보는 바로 이웃
        [TestCase(2, 5, false)]  // 같은 셀
        [TestCase(4, 5, false)]  // 두 칸 거리
        [TestCase(1, 5, false)]  // 뒤쪽 이웃
        [TestCase(2, 6, false)]  // 다른 축의 이웃
        [TestCase(3, 6, false)]  // 대각선
        public void X축_포트는_바라보는_바로_이웃_셀에_있어야_맞물린다(int secondX, int secondY, bool expected)
        {
            var first = new RoadPort(
                new Vector2Int(2, 5),
                RoadPortSide.PositiveX,
                2);

            var second = new RoadPort(
                new Vector2Int(secondX, secondY),
                RoadPortSide.NegativeX,
                2);

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(first, second),
                Is.EqualTo(expected));

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(second, first),
                Is.EqualTo(expected));
        }

        [TestCase(2, 6, true)]   // 바라보는 바로 이웃
        [TestCase(2, 5, false)]  // 같은 셀
        [TestCase(2, 7, false)]  // 두 칸 거리
        [TestCase(2, 4, false)]  // 뒤쪽 이웃
        [TestCase(3, 5, false)]  // 다른 축의 이웃
        [TestCase(3, 6, false)]  // 대각선
        public void Y축_포트도_바라보는_바로_이웃_셀에_있어야_맞물린다(int secondX, int secondY, bool expected)
        {
            var first = new RoadPort(
                new Vector2Int(2, 5),
                RoadPortSide.PositiveY,
                2);

            var second = new RoadPort(
                new Vector2Int(secondX, secondY),
                RoadPortSide.NegativeY,
                2);

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(first, second),
                Is.EqualTo(expected));

            Assert.That(
                RoadPortMatcher.MatchesLogicalBoundary(second, first),
                Is.EqualTo(expected));
        }
    }
}
