using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Marionet.Core.Windows
{
    public class WindowsSystemPermissions : ISystemPersmissions
    {
        public Task<bool> HasUiAccess()
        {
            bool hasUiAccess = false;

            var token = WindowsIdentity.GetCurrent().Token;
            uint tokenInfoLength = 0;

            Native.Methods.GetTokenInformation(token, Native.TOKEN_INFORMATION_CLASS.TokenUIAccess, IntPtr.Zero, tokenInfoLength, out tokenInfoLength);
            IntPtr tokenInfo = Marshal.AllocHGlobal(token);
            bool success = Native.Methods.GetTokenInformation(token, Native.TOKEN_INFORMATION_CLASS.TokenUIAccess, tokenInfo, tokenInfoLength, out tokenInfoLength);

            if (success)
            {
                int uiAccess = Marshal.ReadInt32(tokenInfo);
                hasUiAccess = uiAccess != 0;
            }

            Marshal.FreeHGlobal(tokenInfo);

            return Task.FromResult(hasUiAccess);
        }

        public Task<bool> IsAdmin()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            return Task.FromResult(isAdmin);
        }
    }
}
