using System.Collections.Immutable;

namespace Marionet.Core
{
    public record Desktop(string Name, ImmutableList<Rectangle> Displays, Rectangle? PrimaryDisplay)
    {
        public Desktop(string Name) : this(Name, ImmutableList<Rectangle>.Empty, null) { }

        public override string ToString()
        {
            return Name;
        }
    }
}
