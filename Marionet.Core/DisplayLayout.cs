using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Marionet.Core
{
    public class DisplayLayout
    {

        public DisplayLayout(IEnumerable<Desktop> desktops, IDictionary<string, int> yOffsets)
        {
            if (desktops == null) throw new ArgumentNullException(nameof(desktops));
            if (yOffsets == null) throw new ArgumentNullException(nameof(yOffsets));

            int xOffset = 0;
            List<Rectangle> displayRectangles = new List<Rectangle>();
            Dictionary<Rectangle, Desktop> displayDesktops = new Dictionary<Rectangle, Desktop>();
            Dictionary<Desktop, Point> desktopOrigins = new Dictionary<Desktop, Point>();
            Dictionary<Rectangle, string> displayIds = new Dictionary<Rectangle, string>();
            Dictionary<string, Rectangle> displayById = new Dictionary<string, Rectangle>();

            foreach (Desktop desktop in desktops)
            {
                int desktopMinLeft = desktop.Displays.Select(d => d.Left).DefaultIfEmpty(0).Min();
                int desktopMaxRight = desktop.Displays.Select(d => d.Right).DefaultIfEmpty(0).Max();
                int desktopWidth = desktopMaxRight - desktopMinLeft;
                int desktopLeftOffset = -desktopMinLeft;

                if (!yOffsets.TryGetValue(desktop.Name, out int desktopYOffset))
                {
                    desktopYOffset = 0;
                }

                Point desktopOrigin = new Point(xOffset + desktop.PrimaryDisplay.GetValueOrDefault().X + desktopLeftOffset, desktopYOffset);
                desktopOrigins.Add(desktop, desktopOrigin);

                foreach (Rectangle display in desktop.Displays)
                {
                    var displayId = GetDisplayId(desktop, display);
                    Rectangle rect = display.Offset(desktopOrigin.X, desktopYOffset);
                    displayRectangles.Add(rect);
                    displayDesktops.Add(rect, desktop);
                    displayIds.Add(rect, displayId);
                    displayById.Add(displayId, rect);
                }

                xOffset += desktopWidth;
            }

            DisplayRectangles = displayRectangles.ToImmutableList();
            DisplayDesktops = displayDesktops.ToImmutableDictionary();
            DesktopOrigins = desktopOrigins.ToImmutableDictionary();
            DisplayIds = displayIds.ToImmutableDictionary();
            DisplayById = displayById.ToImmutableDictionary();
        }

        public ImmutableList<Rectangle> DisplayRectangles { get; private set; }

        public ImmutableDictionary<Rectangle, Desktop> DisplayDesktops { get; private set; }

        public ImmutableDictionary<Desktop, Point> DesktopOrigins { get; private set; }

        public ImmutableDictionary<Rectangle, string> DisplayIds { get; private set; }

        public ImmutableDictionary<string, Rectangle> DisplayById { get; private set; }

        public static string GetDisplayId(Desktop desktop, Rectangle display) => $"{desktop}--${display}";

        public (Desktop desktop, Rectangle display)? FindPoint(Point point)
        {
            Rectangle? rect = null;
            foreach (Rectangle r in DisplayRectangles)
            {
                if (r.Contains(point))
                {
                    rect = r;
                    break;
                }
            }

            if (rect.HasValue)
            {
                return (DisplayDesktops[rect.Value], rect.Value);
            }

            return null;
        }
    }
}
