using System.Collections.Generic;

namespace Marionet.Core.LocalState
{
    internal record Controlled(HashSet<string> By) : State;
}
