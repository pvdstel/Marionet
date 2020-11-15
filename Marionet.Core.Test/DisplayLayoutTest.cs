using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Marionet.Core.Test
{
    public class DisplayLayoutTest
    {
        [Fact]
        public void TestNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new DisplayLayout(null);
            });
        }

        [Fact]
        public void TestEmpty()
        {
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", new List<Rectangle>().AsReadOnly(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops);

            Assert.Empty(displayLayout.DisplayRectangles);
        }

        [Fact]
        public void TestOneDisplay()
        {
            var r = new Rectangle(0, 0, 10, 10);
            List<Desktop> desktops = new List<Desktop>()
            {
                new Desktop("test-1", new List<Rectangle>() { r }.AsReadOnly(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops);

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
                new Desktop("test-1", new List<Rectangle>() { new Rectangle(0, 0, 10,10), new Rectangle(10, 10, 10, 10) }.AsReadOnly(), null),
                new Desktop("test-2", new List<Rectangle>() { new Rectangle(0, 5, 15,15), new Rectangle(15, 0, 15, 15) }.AsReadOnly(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops);

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
                new Desktop("test-2", new List<Rectangle>() { new Rectangle(0, 5, 15,15), new Rectangle(15, 0, 15, 15) }.AsReadOnly(), null),
                new Desktop("test-1", new List<Rectangle>() { new Rectangle(0, 0, 10,10), new Rectangle(10, 10, 10, 10) }.AsReadOnly(), null),
            };

            DisplayLayout displayLayout = new DisplayLayout(desktops);

            Assert.Equal(4, displayLayout.DisplayRectangles.Count);
            Assert.Equal(new Point(0, 0), displayLayout.DesktopOrigins[desktops[0]]);
            Assert.Equal(new Point(30, 0), displayLayout.DesktopOrigins[desktops[1]]);

            Assert.Equal(new Rectangle(0, 5, 15, 15), displayLayout.DisplayRectangles[0]);
            Assert.Equal(new Rectangle(15, 0, 15, 15), displayLayout.DisplayRectangles[1]);
            Assert.Equal(new Rectangle(30, 0, 10, 10), displayLayout.DisplayRectangles[2]);
            Assert.Equal(new Rectangle(40, 10, 10, 10), displayLayout.DisplayRectangles[3]);
        }
    }
}
