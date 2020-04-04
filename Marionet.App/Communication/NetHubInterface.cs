using Marionet.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Marionet.App.Communication
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Required for contract generation.")]
    public class NetHubInterface
    {
        public NetHubInterface()
        {
            Identify = null!;
            ChangeDisplays = null!;
        }

        public readonly Func<string, List<string>, Task<IdentifyResult>> Identify;

        public readonly Func<List<Rectangle>, Task> ChangeDisplays;
    }
}
