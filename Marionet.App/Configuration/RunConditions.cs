using System.Collections.Generic;

namespace Marionet.App.Configuration
{
    public class RunConditions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This is a serializable class.")]
        public List<string> AllowedSsids { get; set; } = new List<string>();

        public bool BlockAll { get; set; } = false;
    }
}
