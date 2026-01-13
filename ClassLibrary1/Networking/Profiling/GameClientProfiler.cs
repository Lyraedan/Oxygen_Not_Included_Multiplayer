#if DEBUG
using ImGuiNET;
#endif
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ONI_MP.Networking.Profiling
{
    public static class GameClientProfiler
    {
        private static bool poppedOut = false;

        public static bool Enabled = true;

        // Last poll
        public static int LastMessageCount;
        public static int LastBytes;
        public static long LastTicks;

        // Peaks
        public static int MaxMessages;
        public static int MaxBytes;
        public static long MaxTicks;

        // Rolling averages
        static int samples;
        public static double AvgMs;
        public static double AvgBytes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Begin()
        {
#if RELEASE
            return 0;
#endif

            return Enabled ? Stopwatch.GetTimestamp() : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void End(long startTicks, int msgCount, int bytes)
        {
#if RELEASE
            return;
#endif

            if (!Enabled)
                return;

            long ticks = Stopwatch.GetTimestamp() - startTicks;

            LastMessageCount = msgCount;
            LastBytes = bytes;
            LastTicks = ticks;

            MaxMessages = Math.Max(MaxMessages, msgCount);
            MaxBytes = Math.Max(MaxBytes, bytes);
            MaxTicks = Math.Max(MaxTicks, ticks);

            samples++;
            double ms = ticks * 1000.0 / Stopwatch.Frequency;
            AvgMs += (ms - AvgMs) / samples;
            AvgBytes += (bytes - AvgBytes) / samples;
        }

        public static void Reset()
        {
#if RELEASE
            return;
#endif

            samples = 0;
            AvgMs = 0;
            AvgBytes = 0;
            MaxMessages = 0;
            MaxBytes = 0;
            MaxTicks = 0;
        }

#if DEBUG
        private static void TogglePopout()
        {
            poppedOut = !poppedOut;
        }

        public static void DrawImGuiPopout()
        {
            if (!poppedOut)
                return;

            if (!ImGui.Begin("GameClient Network Profiler", ref poppedOut))
                return;

            DrawHeader();
            DrawBody();

            ImGui.End();
        }

        public static void DrawImGuiInTab()
        {
            DrawHeader();
            ImGui.SameLine();
            if (ImGui.Button("Pop out"))
                TogglePopout();
            DrawBody();
        }

        private static void DrawHeader()
        {
            ImGui.Checkbox("Enabled", ref Enabled);

            ImGui.SameLine();
            if (ImGui.Button("Reset"))
                Reset();
        }

        private static void DrawBody()
        {
            ImGui.Separator();

            ImGui.Text("Last Poll");
            ImGui.BulletText($"Messages: {LastMessageCount}");
            ImGui.BulletText($"Bytes: {LastBytes}");
            ImGui.BulletText($"Time: {(LastTicks * 1000.0 / Stopwatch.Frequency):F3} ms");

            ImGui.Separator();

            ImGui.Text("Averages");
            ImGui.BulletText($"Avg Time: {AvgMs:F3} ms");
            ImGui.BulletText($"Avg Bytes: {AvgBytes:F0}");

            ImGui.Separator();

            ImGui.Text("Peaks");
            ImGui.BulletText($"Max Messages: {MaxMessages}");
            ImGui.BulletText($"Max Bytes: {MaxBytes}");
            ImGui.BulletText($"Max Time: {(MaxTicks * 1000.0 / Stopwatch.Frequency):F3} ms");
        }
#endif
    }
}
