using System;
using System.IO;
using System.Runtime.InteropServices;
using ONI_MP.DebugTools;
using UnityEngine;

namespace ONI_MP.Networking.Platforms.EOS
{
    public static class EOSLoader
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("libdl")] // For Linux/macOS
        private static extern IntPtr dlopen(string fileName, int flags);

        private static bool loaded = false;

        public static void LoadNativeLibrary()
        {
            if (loaded) return;
            loaded = true;

            string modPath = Path.GetDirectoryName(typeof(EOSLoader).Assembly.Location);
            string libPath = GetLibraryPath(modPath);

            if (!File.Exists(libPath))
            {
                DebugConsole.LogError($"[EOSLoader] EOS native library not found at: {libPath}");
                return;
            }

            DebugConsole.Log($"[EOSLoader] Attempting to load native library: {libPath}");

            IntPtr handle = IntPtr.Zero;

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                handle = LoadLibrary(libPath);
            else
                handle = dlopen(libPath, 2); // RTLD_NOW = 2

            if (handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                DebugConsole.LogError($"[EOSLoader] Failed to load native library. Error code: {error}");
            }
            else
            {
                DebugConsole.Log($"[EOSLoader] Successfully loaded native EOS library from: {libPath}");
            }
        }

        private static string GetLibraryPath(string basePath)
        {
            string platformFolder = Path.Combine(basePath, "eos");

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                bool is64Bit = IntPtr.Size == 8;
                return Path.Combine(platformFolder, is64Bit ? "EOSSDK-Win64-Shipping.dll" : "EOSSDK-Win32-Shipping.dll");
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                return Path.Combine(platformFolder, "libEOSSDK-Mac-Shipping.dylib");
            }
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
            {
                bool isArm64 = SystemInfo.processorType.ToLower().Contains("arm");
                return Path.Combine(platformFolder, isArm64 ? "libEOSSDK-LinuxArm64-Shipping.so" : "libEOSSDK-Linux-Shipping.so");
            }

            throw new PlatformNotSupportedException("EOSLoader: Unsupported platform.");
        }
    }
}
