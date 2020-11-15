using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Marionet.Core
{
    internal class DisplayLayout
    {
        private readonly List<Rectangle> displayRectangles = new List<Rectangle>();
        private readonly Dictionary<Rectangle, Desktop> displayDesktops = new Dictionary<Rectangle, Desktop>();
        private readonly Dictionary<Desktop, Point> desktopOrigins = new Dictionary<Desktop, Point>();
        private readonly Dictionary<Rectangle, string> displayIds = new Dictionary<Rectangle, string>();
        private readonly Dictionary<string, Rectangle> displayById = new Dictionary<string, Rectangle>();

        public DisplayLayout(IEnumerable<Desktop> desktops)
        {
            Initialize(desktops);
        }

        public ReadOnlyCollection<Rectangle> DisplayRectangles => displayRectangles.AsReadOnly();

        public ReadOnlyDictionary<Rectangle, Desktop> DisplayDesktops => new ReadOnlyDictionary<Rectangle, Desktop>(displayDesktops);

        public ReadOnlyDictionary<Desktop, Point> DesktopOrigins => new ReadOnlyDictionary<Desktop, Point>(desktopOrigins);
        
        public ReadOnlyDictionary<Rectangle, string> DisplayIds => new ReadOnlyDictionary<Rectangle, string>(displayIds);

        public ReadOnlyDictionary<string, Rectangle> DisplayById => new ReadOnlyDictionary<string, Rectangle>(displayById);

        public (Desktop desktop, Rectangle display)? FindPoint(Point point)
        {
            Rectangle? rect = null;
            foreach (Rectangle r in displayRectangles)
            {
                if (r.Contains(point))
                {
                    rect = r;
                    break;
                }
            }

            if (rect.HasValue)
            {
                return (displayDesktops[rect.Value], rect.Value);
            }

            return null;
        }

        private void Initialize(IEnumerable<Desktop> desktops)
        {
            int offset = 0;

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
                    var displayId = $"{desktop}--${display}";
                    Rectangle rect = display.Offset(offset + desktopLeftOffset, 0);
                    displayRectangles.Add(rect);
                    displayDesktops.Add(rect, desktop);
                    displayIds.Add(rect, displayId);
                    displayById.Add(displayId, rect);
                }

                offset += desktopWidth;
            }
        }
    }
}
