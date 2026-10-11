using System;
using NUnit.Framework;
using UnityEngine;
using YouSaidLeft.Core;

namespace YouSaidLeft.Tests
{
    public class RoadPortResolverTests
    {
        [Test]
        public void 무회전_포트는_배치와_로컬정의에서_해석된다()
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), 1);

            var definition = new RoadPortDefinition(
                new Vector2(0.5f, 0f),
                RoadPortSide.PositiveX,
                0);

            var actual = RoadPortResolver.ResolveUnrotated(
                placement, definition);

            Assert.That(actual.Port.CellCoordinates,
                Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(actual.ContactPoint,
                Is.EqualTo(new Vector2(2.5f, 5f)));
            Assert.That(actual.Port.Side,
                Is.EqualTo(RoadPortSide.PositiveX));
            Assert.That(actual.Port.BoundaryLevel,
                Is.EqualTo(1));
        }

        [Test]
        public void 배치_셀을_옮기면_접속점도_같이_이동한다()
        {
            var definition = new RoadPortDefinition(
                new Vector2(0.5f, 0f),
                RoadPortSide.PositiveX,
                0);

            var first = RoadPortResolver.ResolveUnrotated(
                new RoadModulePlacement(new Vector2Int(2, 5), 1),
                definition);

            var moved = RoadPortResolver.ResolveUnrotated(
                new RoadModulePlacement(new Vector2Int(7, 2), 1),
                definition);

            Assert.That(first.ContactPoint,
                Is.EqualTo(new Vector2(2.5f, 5f)));
            Assert.That(moved.Port.CellCoordinates,
                Is.EqualTo(new Vector2Int(7, 2)));
            Assert.That(moved.ContactPoint,
                Is.EqualTo(new Vector2(7.5f, 2f)));
            Assert.That(moved.Port.Side,
                Is.EqualTo(RoadPortSide.PositiveX));
            Assert.That(moved.Port.BoundaryLevel,
                Is.EqualTo(1));
        }

        [TestCase(3, 2, 5)]
        [TestCase(3, -2, 1)]
        [TestCase(-3, 2, -1)]
        [TestCase(int.MaxValue, 0, int.MaxValue)]
        [TestCase(int.MinValue, 0, int.MinValue)]
        [TestCase(int.MaxValue - 1, 1, int.MaxValue)]
        [TestCase(int.MinValue + 1, -1, int.MinValue)]
        public void 끝점_높이는_기준높이와_로컬오프셋의_합이다(int baseLevel, int levelOffset, int expectedLevel)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), baseLevel);

            var definition = new RoadPortDefinition(
                new Vector2(0.5f, 0f),
                RoadPortSide.PositiveX,
                levelOffset);

            var actual = RoadPortResolver.ResolveUnrotated(
                placement, definition);

            Assert.That(actual.Port.BoundaryLevel,
                Is.EqualTo(expectedLevel));
            Assert.That(actual.Port.CellCoordinates,
                Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(actual.ContactPoint,
                Is.EqualTo(new Vector2(2.5f, 5f)));
            Assert.That(actual.Port.Side,
                Is.EqualTo(RoadPortSide.PositiveX));
        }

        [TestCase(-0.5f, 0f, RoadPortSide.NegativeX, 1.5f, 5f)]
        [TestCase(0f, 0.5f, RoadPortSide.PositiveY, 2f, 5.5f)]
        [TestCase(0f, -0.5f, RoadPortSide.NegativeY, 2f, 4.5f)]
        public void 무회전_해석은_로컬위치와_방향을_반영한다(
            float localX, float localY, RoadPortSide side,
            float expectedX, float expectedY)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), 1);

            var definition = new RoadPortDefinition(
                new Vector2(localX, localY),
                side,
                0);

            var actual = RoadPortResolver.ResolveUnrotated(
                placement, definition);

            Assert.That(actual.Port.Side, Is.EqualTo(side));
            Assert.That(actual.ContactPoint,
                Is.EqualTo(new Vector2(expectedX, expectedY)));
            Assert.That(actual.Port.CellCoordinates,
                Is.EqualTo(new Vector2Int(2, 5)));
            Assert.That(actual.Port.BoundaryLevel, Is.EqualTo(1));
        }

        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.NaN)]
        public void 로컬좌표가_NaN이면_해석을_거부한다(float localX, float localY)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), 1);

            var definition = new RoadPortDefinition(
                new Vector2(localX, localY),
                RoadPortSide.PositiveX,
                0);

            Assert.Throws<ArgumentException>(() =>
                RoadPortResolver.ResolveUnrotated(
                    placement, definition));
        }

        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(float.NegativeInfinity, 0f)]
        [TestCase(0f, float.PositiveInfinity)]
        [TestCase(0f, float.NegativeInfinity)]
        public void 로컬좌표가_무한대이면_해석을_거부한다(float localX, float localY)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), 1);

            var definition = new RoadPortDefinition(
                new Vector2(localX, localY),
                RoadPortSide.PositiveX,
                0);

            Assert.Throws<ArgumentException>(() =>
                RoadPortResolver.ResolveUnrotated(
                    placement, definition));
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void 정의되지_않은_방향이면_해석을_거부한다(int sideValue)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), 1);

            var definition = new RoadPortDefinition(
                new Vector2(0.5f, 0f),
                (RoadPortSide)sideValue,
                0);

            Assert.Throws<ArgumentException>(() =>
                RoadPortResolver.ResolveUnrotated(
                    placement, definition));
        }

        [TestCase(int.MaxValue, 1)]
        [TestCase(int.MinValue, -1)]
        public void 끝점_높이가_int_범위를_벗어나면_해석을_거부한다(int baseLevel, int levelOffset)
        {
            var placement = new RoadModulePlacement(
                new Vector2Int(2, 5), baseLevel);

            var definition = new RoadPortDefinition(
                new Vector2(0.5f, 0f),
                RoadPortSide.PositiveX,
                levelOffset);

            Assert.Throws<OverflowException>(() =>
                RoadPortResolver.ResolveUnrotated(
                    placement, definition));
        }

        [TestCase(-2, -5, 0.5f, 0f, -1.5f, -5f)]
        [TestCase(2, 5, 1.25f, -0.75f, 3.25f, 4.25f)]
        public void 무회전_해석은_셀좌표와_유한한_로컬위치를_반영한다(
            int cellX, int cellY,
            float localX, float localY,
            float expectedX, float expectedY)
        {
            var cell = new Vector2Int(cellX, cellY);
            var placement = new RoadModulePlacement(cell, 1);

            var definition = new RoadPortDefinition(
                new Vector2(localX, localY),
                RoadPortSide.PositiveX,
                0);

            var actual = RoadPortResolver.ResolveUnrotated(
                placement, definition);

            Assert.That(actual.Port.CellCoordinates, Is.EqualTo(cell));
            Assert.That(actual.ContactPoint,
                Is.EqualTo(new Vector2(expectedX, expectedY)));
            Assert.That(actual.Port.Side, Is.EqualTo(RoadPortSide.PositiveX));
            Assert.That(actual.Port.BoundaryLevel, Is.EqualTo(1));
        }
    }
}
