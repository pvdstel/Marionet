using System;

namespace Marionet.Core
{
    public static class Extensions
    {
        public static string NormalizeDesktopName(this string name)
        {
            return name?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
