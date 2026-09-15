using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests.EditMode
{
    public sealed class DistrictSelectionGeometryTests
    {
        private static readonly Vector2[] Diamond = {
            new(0, 5), new(5, 0), new(10, 5), new(5, 10) };

        [Test]
        public void DragDirectionDoesNotChangeTheRectangle()
        {
            var expected = new Rect(10, 20, 70, 40);
            Assert.AreEqual(expected, DistrictSelectionGeometry.Rectangle(new(10, 20), new(80, 60)));
            Assert.AreEqual(expected, DistrictSelectionGeometry.Rectangle(new(80, 60), new(10, 20)));
            Assert.AreEqual(expected, DistrictSelectionGeometry.Rectangle(new(10, 60), new(80, 20)));
        }
        [Test]
        public void EmptyCornerOfIsometricBoundsIsNotSelected()
        {
            Assert.IsFalse(DistrictSelectionGeometry.Overlaps(new Rect(0, 0, 1, 1), Diamond));
        }
        [Test]
        public void CrossingFootprintIsSelectedEvenWithoutAnyCornerInside()
        {
            Assert.IsTrue(DistrictSelectionGeometry.Overlaps(new Rect(-1, 4.5f, 12, 1), Diamond));
        }
        [Test]
        public void RectangleInsideLargeObjectSelectsIt()
        {
            Assert.IsTrue(DistrictSelectionGeometry.Overlaps(new Rect(4, 4, 2, 2), Diamond));
        }
        [Test]
        public void ContactAtTheRectangleEdgeCounts()
        {
            Assert.IsTrue(DistrictSelectionGeometry.Overlaps(new Rect(10, 4, 1, 2), Diamond));
            Assert.IsFalse(DistrictSelectionGeometry.Overlaps(new Rect(10.1f, 4, 1, 2), Diamond));
        }
    }
}
