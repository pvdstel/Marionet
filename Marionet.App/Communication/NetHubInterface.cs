using Marionet.Core;
using System;
using System.Collections.Immutable;
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

        public readonly Func<string, ImmutableList<string>, Task<IdentifyResult>> Identify;

        public readonly Func<ImmutableList<Rectangle>, Task> ChangeDisplays;
    }
}
