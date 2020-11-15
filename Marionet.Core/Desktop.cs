using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Marionet.Core
{
    public record Desktop(string Name, ReadOnlyCollection<Rectangle> Displays, Rectangle? PrimaryDisplay)
    {
        public Desktop(string Name) : this(Name, new List<Rectangle>().AsReadOnly(), null) { }

        public override string ToString()
        {
            return Name;
        }
    }
}
