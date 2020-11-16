using System.Collections.Immutable;

namespace Marionet.Core.LocalState
{
    internal record Controlled(ImmutableHashSet<string> By) : State;
}
