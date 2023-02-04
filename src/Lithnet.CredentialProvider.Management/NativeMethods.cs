using System;
using System.Runtime.InteropServices;

namespace Lithnet.CredentialProvider.Management
{
    internal static class NativeMethods
    {
        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsUserAnAdmin();

        public static void ThrowIfNotAdmin()
        {
            if (!IsUserAnAdmin())
            {
                throw new UnauthorizedAccessException("This cmdlet requires administrative privileges to run");
            }
        }
    }
}
