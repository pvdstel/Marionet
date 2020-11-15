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
    }
}
