using System;
using System.Collections.Generic;
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
    }
}
