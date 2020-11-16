using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Marionet.Core
{
    public class DisplayLayout
    {

        public DisplayLayout(IEnumerable<Desktop> desktops)
        {
            if (desktops == null) throw new ArgumentNullException(nameof(desktops));

            int offset = 0;
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

                Point desktopOrigin = new Point(offset + desktop.PrimaryDisplay.GetValueOrDefault().X + desktopLeftOffset, 0);
                desktopOrigins.Add(desktop, desktopOrigin);

                foreach (Rectangle display in desktop.Displays)
                {
                    var displayId = GetDisplayId(desktop, display);
                    Rectangle rect = display.Offset(desktopOrigin.X, 0);
                    displayRectangles.Add(rect);
                    displayDesktops.Add(rect, desktop);
                    displayIds.Add(rect, displayId);
                    displayById.Add(displayId, rect);
                }

                offset += desktopWidth;
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
