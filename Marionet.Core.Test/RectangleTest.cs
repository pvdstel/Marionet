using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Marionet.Core.Test
{
    public class RectangleTest
    {
        [Fact]
        public void TestIO()
        {
            var r = new Rectangle(7, 11, 17, 23);
            Assert.Equal(7, r.X);
            Assert.Equal(11, r.Y);
            Assert.Equal(17, r.Width);
            Assert.Equal(23, r.Height);
        }

        [Fact]
        public void TestAuxiliary()
        {
            var r = new Rectangle(7, 11, 17, 23);
            Assert.Equal(7, r.Left);
            Assert.Equal(11, r.Top);
            Assert.Equal(24, r.Right);
            Assert.Equal(34, r.Bottom);
        }

        private class OffsetTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { 0, 0, new Rectangle(0, 0, 13, 17), new Rectangle(0, 0, 13, 17) };
                yield return new object[] { 0, 0, new Rectangle(0, 0, 23, 29), new Rectangle(0, 0, 23, 29) };
                yield return new object[] { 7, 2, new Rectangle(0, 0, 13, 17), new Rectangle(7, 2, 13, 17) };
                yield return new object[] { -59, 29, new Rectangle(23, 5, 13, 17), new Rectangle(-36, 34, 13, 17) };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(OffsetTestData))]
        public void TestOffset(int byX, int byY, Rectangle first, Rectangle expected)
        {
            var actual = first.Offset(byX, byY);
            Assert.Equal(expected, actual);
        }

        private class ContainsTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var r = new Rectangle(0, 0, 10, 10);
                yield return new object[] { 0, 0, r, true };
                yield return new object[] { 10, 10, r, false };
                yield return new object[] { -1, -1, r, false };
                yield return new object[] { 11, 11, r, false };
                yield return new object[] { 1, 1, r, true };
                yield return new object[] { 9, 9, r, true };
                yield return new object[] { 10, 1, r, false };
                yield return new object[] { 1, 10, r, false };
                yield return new object[] { 9, 1, r, true };
                yield return new object[] { 1, 9, r, true };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(ContainsTestData))]
        public void TestContains(int x, int y, Rectangle first, bool expected)
        {
            var actual = first.Contains(new Point(x, y));
            Assert.Equal(expected, actual);
        }

        private class ClampTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var r = new Rectangle(0, 0, 10, 10);
                yield return new object[] { 0, 0, r, new Point(0, 0) };
                yield return new object[] { 10, 10, r, new Point(9, 9) };
                yield return new object[] { -1, -1, r, new Point(0, 0) };
                yield return new object[] { 11, 11, r, new Point(9, 9) };
                yield return new object[] { 1, 1, r, new Point(1, 1) };
                yield return new object[] { 9, 9, r, new Point(9, 9) };
                yield return new object[] { 1, 10, r, new Point(1, 9) };
                yield return new object[] { 10, 1, r, new Point(9, 1) };
                yield return new object[] { -1, 9, r, new Point(0, 9) };
                yield return new object[] { 9, -1, r, new Point(9, 0) };
                yield return new object[] { 1, 9, r, new Point(1, 9) };
                yield return new object[] { 9, 1, r, new Point(9, 1) };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(ClampTestData))]
        public void TestClamp(int x, int y, Rectangle first, Point expected)
        {
            var actual = first.Clamp(new Point(x, y));
            Assert.Equal(expected, actual);
        }
    }
}
