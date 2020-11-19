using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Marionet.Core.Test
{
    public class DisplayLayoutTest
    {
        private static readonly ImmutableDictionary<string, int> defaultYOffsets = ImmutableDictionary<string, int>.Empty;

        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DisplayLayout(null, defaultYOffsets);
            });
        }

        [Fact]
        public void TestEmpty()
        {
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", ImmutableList<Rectangle>.Empty, null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops, defaultYOffsets);

            Assert.Empty(displayLayout.DisplayRectangles);
        }

        [Fact]
        public void TestOneDisplay()
        {
            var r = new Rectangle(0, 0, 10, 10);
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", ImmutableList<Rectangle>.Empty.Add(r), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops, defaultYOffsets);

            Assert.Single(displayLayout.DisplayRectangles);
            Assert.Equal(displayLayout.DisplayIds.Values.ToList(), new List<string>() { DisplayLayout.GetDisplayId(desktops[0], r) });
            Assert.Equal(displayLayout.DisplayIds.Keys.ToList(), new List<Rectangle>() { r });
            Assert.Equal(displayLayout.DisplayById.Keys.ToList(), new List<string>() { DisplayLayout.GetDisplayId(desktops[0], r) });
            Assert.Equal(displayLayout.DisplayById.Values.ToList(), new List<Rectangle>() { r });
        }

        [Fact]
        public void TestMultiDisplay()
        {
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", new List<Rectangle>() { new Rectangle(0, 0, 10, 10), new Rectangle(10, 10, 10, 10) }.ToImmutableList(), null),
                new Desktop("test-2", new List<Rectangle>() { new Rectangle(0, 5, 15, 15), new Rectangle(15, 0, 15, 15) }.ToImmutableList(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops, defaultYOffsets);

            Assert.Equal(4, displayLayout.DisplayRectangles.Count);
            Assert.Equal(new Point(0, 0), displayLayout.DesktopOrigins[desktops[0]]);
            Assert.Equal(new Point(20, 0), displayLayout.DesktopOrigins[desktops[1]]);

            Assert.Equal(new Rectangle(0, 0, 10, 10), displayLayout.DisplayRectangles[0]);
            Assert.Equal(new Rectangle(10, 10, 10, 10), displayLayout.DisplayRectangles[1]);
            Assert.Equal(new Rectangle(20, 5, 15, 15), displayLayout.DisplayRectangles[2]);
            Assert.Equal(new Rectangle(35, 0, 15, 15), displayLayout.DisplayRectangles[3]);
        }

        [Fact]
        public void TestMultiDisplayReverse()
        {
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-2", new List<Rectangle>() { new Rectangle(0, 5, 15, 15), new Rectangle(15, 0, 15, 15) }.ToImmutableList(), null),
                new Desktop("test-1", new List<Rectangle>() { new Rectangle(0, 0, 10, 10), new Rectangle(10, 10, 10, 10) }.ToImmutableList(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops, defaultYOffsets);

            Assert.Equal(4, displayLayout.DisplayRectangles.Count);
            Assert.Equal(new Point(0, 0), displayLayout.DesktopOrigins[desktops[0]]);
            Assert.Equal(new Point(30, 0), displayLayout.DesktopOrigins[desktops[1]]);

            Assert.Equal(new Rectangle(0, 5, 15, 15), displayLayout.DisplayRectangles[0]);
            Assert.Equal(new Rectangle(15, 0, 15, 15), displayLayout.DisplayRectangles[1]);
            Assert.Equal(new Rectangle(30, 0, 10, 10), displayLayout.DisplayRectangles[2]);
            Assert.Equal(new Rectangle(40, 10, 10, 10), displayLayout.DisplayRectangles[3]);
        }

        [Fact]
        public void TestMultiDisplayYOffsets()
        {
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", new List<Rectangle>() { new Rectangle(0, 0, 10, 10), new Rectangle(10, 10, 10, 10) }.ToImmutableList(), null),
                new Desktop("test-2", new List<Rectangle>() { new Rectangle(0, 5, 15,15), new Rectangle(15, 0, 15, 15) }.ToImmutableList(), null),
            };

            Dictionary<string, int> yOffsets = new Dictionary<string, int>()
            {
                { "test-1", 5 },
                { "test-2", -2 },
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops, yOffsets);

            Assert.Equal(4, displayLayout.DisplayRectangles.Count);
            Assert.Equal(new Point(0, 5), displayLayout.DesktopOrigins[desktops[0]]);
            Assert.Equal(new Point(20, -2), displayLayout.DesktopOrigins[desktops[1]]);

            Assert.Equal(new Rectangle(0, 5, 10, 10), displayLayout.DisplayRectangles[0]);
            Assert.Equal(new Rectangle(10, 15, 10, 10), displayLayout.DisplayRectangles[1]);
            Assert.Equal(new Rectangle(20, 3, 15, 15), displayLayout.DisplayRectangles[2]);
            Assert.Equal(new Rectangle(35, -2, 15, 15), displayLayout.DisplayRectangles[3]);
        }
    }
}
