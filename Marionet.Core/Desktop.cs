using System.Collections.Generic;

namespace Marionet.Core
{
    public class Desktop
    {
        public string Name { get; set; } = string.Empty;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Should be replaceable.")]
        public List<Rectangle> Displays { get; set; } = new List<Rectangle>();

        public Rectangle? PrimaryDisplay { get; set; }

        public override string ToString()
        {
            return $"Desktop({Name})";
        }
    }
}
