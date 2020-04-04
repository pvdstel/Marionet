using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    public class IdentifyResult
    {
        public string? DesktopName { get; set; }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This is a DTO class member.")]
        public List<string>? Desktops { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(DesktopName) &&
                Desktops != null;
        }
    }
}
