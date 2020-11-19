using System;
using System.Collections.Immutable;
using System.Linq;

namespace Marionet.App.Configuration
{
    internal record DesktopManagementState(ImmutableList<string> Desktops, ImmutableDictionary<string, int> YOffsets) : IEquatable<DesktopManagementState>
    {
        public static readonly DesktopManagementState Default = new DesktopManagementState(
            ImmutableList<string>.Empty,
            ImmutableDictionary<string, int>.Empty
        );

        public virtual bool Equals(DesktopManagementState? other)
        {
            if (other == null) return false;

            if (!Desktops.SequenceEqual(other.Desktops)) return false;

            if (YOffsets.Count != other.YOffsets.Count || YOffsets.Except(other.YOffsets).Any()) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Desktops.GetHashCode(), YOffsets.GetHashCode());
        }
    }
}
