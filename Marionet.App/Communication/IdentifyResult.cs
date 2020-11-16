using System.Collections.Generic;

namespace Marionet.App.Communication
{
    public class IdentifyResult
    {
        public string? DesktopName { get; set; }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Would-be immutable, but that's not supported b the transport protocol.")]
        public List<string>? Desktops { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Would-be immutable, but that's not supported b the transport protocol.")]
        public Dictionary<string, int>? YOffsets { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DesktopName) &&
                Desktops != null &&
                YOffsets != null;
        }
    }
}
