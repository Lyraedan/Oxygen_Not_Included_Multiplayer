using System;
using System.IO;
using System.Runtime.InteropServices;
using ONI_MP.DebugTools;

namespace ONI_MP.Networking.Platforms.EOS
{
    public static class EOSLoader
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static bool loaded = false;

        public static void LoadNativeLibrary()
        {
            if (loaded) return;
            loaded = true;

            string modPath = Path.GetDirectoryName(typeof(EOSLoader).Assembly.Location);
            string dllPath = Path.Combine(modPath, "eos", "EOSSDK-Win64-Shipping.dll");

            if (!File.Exists(dllPath))
            {
                DebugConsole.LogError($"[EOSLoader] Native EOS DLL not found at: {dllPath}");
                return;
            }

            IntPtr handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                DebugConsole.LogError($"[EOSLoader] Failed to load EOS native DLL. Error code: {error}");
            }
            else
            {
                DebugConsole.Log($"[EOSLoader] Successfully loaded native EOS DLL from: {dllPath}");
            }
        }
    }
}
