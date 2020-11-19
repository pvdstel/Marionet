using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Marionet.Core.Test
{
    public class PointTest
    {
        [Fact]
        public void TestIO()
        {
            var r = new Point(7, 11);
            Assert.Equal(7, r.X);
            Assert.Equal(11, r.Y);
        }

        private class OffsetTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { 0, 0, new Point(0, 0), new Point(0, 0) };
                yield return new object[] { 7, 2, new Point(0, 0), new Point(7, 2) };
                yield return new object[] { -59, 29, new Point(23, 5), new Point(-36, 34) };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(OffsetTestData))]
        public void TestOffset(int byX, int byY, Point first, Point expected)
        {
            var actual = first.Offset(byX, byY);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, 0, 3, 4, 5)]
        [InlineData(0, 0, 6, 8, 10)]
        [InlineData(0, 0, -3, 4, 5)]
        [InlineData(0, 0, -6, 8, 10)]
        [InlineData(0, 0, 3, -4, 5)]
        [InlineData(0, 0, 6, -8, 10)]
        [InlineData(0, 0, -3, -4, 5)]
        [InlineData(0, 0, -6, -8, 10)]
        public void TestEuclideanDistance(int x1, int y1, int x2, int y2, double d)
        {
            Assert.Equal(d, new Point(x1, y1).EuclideanDistance(new Point(x2, y2)));
        }
    }
}
