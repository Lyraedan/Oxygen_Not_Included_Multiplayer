using System;
using System.Runtime.CompilerServices;

#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace Shared.Profiling
{
    public static class Profiler
    {
        private static bool Enabled = true;

        private const int HistorySize = 300;

        private static readonly float[] _cpuHistory = new float[HistorySize];
        private static readonly float[] _networkHistory = new float[HistorySize];
        private static readonly float[] _messageHistory = new float[HistorySize];
        private static          int     _historyIndex;
        private static          int     _historyCount;

        private static long LastCpu;
        private static int  LastNetwork;
        private static int  LastMessage;

        private static long PeakCpu;
        private static int  PeakNetwork;
        private static int  PeakMessages;

        private const  double Alpha = 0.05;
        private static double AvgCpu;
        private static double AvgNetwork;
        private static double AvgMessage;

        private static int TotalPolls;

        private static float _cpuGraphMax = 1f;
        private static float _networkGraphMax = 1024f;
        private static float _messageGraphMax = 10f;

        private class HotPathEntry
        {
            public string Key = string.Empty;
            public string FilePath = string.Empty;
            public string MemberName = string.Empty;
            public int    LineNumber;

            public long   Calls;
            public double TotalCpu;
            public double PeakCpu;
            public double AvgCpu;

            public long   TotalNetwork;
            public long   PeakNetwork;
            public double AvgNetwork;
            public bool   HasNetworkData;

            private const double SectionAlpha = 0.08;

            public void Record( double ms )
            {
                Calls++;
                TotalCpu += ms;
                if( ms > PeakCpu )
                    PeakCpu = ms;

                if( Calls == 1 )
                    AvgCpu = ms;
                else
                    AvgCpu += SectionAlpha * ( ms - AvgCpu );
            }

            public void RecordBytes( int bytes )
            {
                HasNetworkData = true;
                TotalNetwork += bytes;
                if( bytes > PeakNetwork )
                    PeakNetwork = bytes;

                if( TotalNetwork == bytes )
                    AvgNetwork = bytes;
                else
                    AvgNetwork += SectionAlpha * ( bytes - AvgNetwork );
            }
        }

        private static readonly Dictionary< string, HotPathEntry > _hotPaths = new( 64 );

        private static List< HotPathEntry > _hotPathsSorted = new();

        private static int _hotPathSortDirty = 1;
        private static int _hotPathFrameAccum;
        private const  int HotPathSortInterval = 30;

        private static bool _poppedOut;
        private static bool _showGraphs = true;

        private static bool   _showHotPaths = true;
        private static int    _hotPathSortColumn = 2;
        private static bool   _hotPathSortAscending;
        private static string _hotPathFilter = string.Empty;

        public struct ProfileScope : IDisposable
        {
            private readonly string? _key;
            private readonly string  _memberName;
            private readonly string  _filePath;
            private readonly int     _lineNumber;
            private          long    _startTicks;

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            internal ProfileScope( string? key, string memberName, string filePath, int lineNumber )
            {
                _key = key;
                _memberName = memberName;
                _filePath = filePath;
                _lineNumber = lineNumber;
                _startTicks = Stopwatch.GetTimestamp();
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            private void End()
            {
                if( _startTicks == 0 )
                    return;

                long   ticks = Stopwatch.GetTimestamp() - _startTicks;
                double ms = ticks * 1000.0 / Stopwatch.Frequency;

                string key = ResolveKey();
                RecordHotPath( key, ms, _memberName, _filePath, _lineNumber );

                _startTicks = 0;
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            private string ResolveKey( string? extra = null )
            {
                string baseKey = _key ?? $"{Path.GetFileNameWithoutExtension( _filePath )}.{_memberName}:{_lineNumber}";
                return extra != null ? $"{baseKey}-{extra}" : baseKey;
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string key )
            {
                if( _startTicks == 0 )
                    return;

                long   ticks = Stopwatch.GetTimestamp() - _startTicks;
                double ms = ticks * 1000.0 / Stopwatch.Frequency;

                string resolvedKey = ResolveKey( key );
                RecordHotPath( resolvedKey, ms, _memberName, _filePath, _lineNumber );

                _startTicks = 0;
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string key, int bytes )
            {
                if( _startTicks == 0 )
                    return;

                long   ticks = Stopwatch.GetTimestamp() - _startTicks;
                double ms = ticks * 1000.0 / Stopwatch.Frequency;

                string resolvedKey = ResolveKey( key );
                RecordHotPath( resolvedKey, ms, bytes, _memberName, _filePath, _lineNumber );

                _startTicks = 0;
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( int messageCount, int bytes ) => End( null, messageCount, bytes );

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string? key, int messageCount, int bytes )
            {
                if( _startTicks == 0 )
                    return;

                long   ticks = Stopwatch.GetTimestamp() - _startTicks;
                double ms = ticks * 1000.0 / Stopwatch.Frequency;

                string resolvedKey = ResolveKey( key );
                RecordHotPath( resolvedKey, ms, _memberName, _filePath, _lineNumber );
                RecordNetworkStats( resolvedKey, ticks, messageCount, bytes );

                _startTicks = 0;
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Dispose() => End();
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static ProfileScope Scope( [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0 )
        {
            return Enabled ? new ProfileScope( null, memberName, filePath, lineNumber ) : default;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static ProfileScope Scope( string key, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0 )
        {
            return Enabled ? new ProfileScope( key, memberName, filePath, lineNumber ) : default;
        }

        private static void RecordHotPath( string key, double ms, string memberName, string filePath, int lineNumber )
        {
            if( !Enabled )
                return;

            if( !_hotPaths.TryGetValue( key, out var entry ) )
            {
                entry = new HotPathEntry
                {
                    Key = key,
                    FilePath = filePath,
                    MemberName = memberName,
                    LineNumber = lineNumber
                };
                _hotPaths[ key ] = entry;
                _hotPathSortDirty = 1;
            }

            entry.Record( ms );
            _hotPathSortDirty = 1;
        }

        private static void RecordHotPath( string key, double ms, int bytes, string memberName, string filePath, int lineNumber )
        {
            if( !Enabled )
                return;

            if( !_hotPaths.TryGetValue( key, out var entry ) )
            {
                entry = new HotPathEntry
                {
                    Key = key,
                    FilePath = filePath,
                    MemberName = memberName,
                    LineNumber = lineNumber
                };
                _hotPaths[ key ] = entry;
                _hotPathSortDirty = 1;
            }

            entry.Record( ms );
            entry.RecordBytes( bytes );
            _hotPathSortDirty = 1;
        }

        internal static void RecordNetworkStats( string key, long ticks, int messageCount, int bytes )
        {
            if( !Enabled )
                return;

            if( _hotPaths.TryGetValue( key, out var entry ) )
                entry.RecordBytes( bytes );

            double ms = ticks * 1000.0 / Stopwatch.Frequency;

            LastMessage = messageCount;
            LastNetwork = bytes;
            LastCpu = ticks;

            PeakMessages = Math.Max( PeakMessages, messageCount );
            PeakNetwork = Math.Max( PeakNetwork,  bytes );
            PeakCpu = Math.Max( PeakCpu,      ticks );

            TotalPolls++;

            if( TotalPolls == 1 )
            {
                AvgCpu = ms;
                AvgNetwork = bytes;
                AvgMessage = messageCount;
            }
            else
            {
                AvgCpu += Alpha * ( ms           - AvgCpu );
                AvgNetwork += Alpha * ( bytes        - AvgNetwork );
                AvgMessage += Alpha * ( messageCount - AvgMessage );
            }

            _cpuHistory[ _historyIndex ] = ( float )ms;
            _networkHistory[ _historyIndex ] = bytes;
            _messageHistory[ _historyIndex ] = messageCount;
            _historyIndex = ( _historyIndex + 1 ) % HistorySize;
            if( _historyCount < HistorySize )
                _historyCount++;

            _cpuGraphMax = Math.Max( ( float )ms,  _cpuGraphMax     * 0.998f );
            _networkGraphMax = Math.Max( bytes,        _networkGraphMax * 0.998f );
            _messageGraphMax = Math.Max( messageCount, _messageGraphMax * 0.998f );

            _cpuGraphMax = Math.Max( _cpuGraphMax,     0.1f );
            _networkGraphMax = Math.Max( _networkGraphMax, 64f );
            _messageGraphMax = Math.Max( _messageGraphMax, 1f );
        }

        public static void Reset()
        {
            LastMessage = 0;
            LastNetwork = 0;
            LastCpu = 0;

            PeakMessages = 0;
            PeakNetwork = 0;
            PeakCpu = 0;

            AvgCpu = 0;
            AvgNetwork = 0;
            AvgMessage = 0;

            TotalPolls = 0;

            _historyIndex = 0;
            _historyCount = 0;
            Array.Clear( _cpuHistory,     0, HistorySize );
            Array.Clear( _networkHistory, 0, HistorySize );
            Array.Clear( _messageHistory, 0, HistorySize );

            _cpuGraphMax = 1f;
            _networkGraphMax = 1024f;
            _messageGraphMax = 10f;

            ResetHotPaths();
        }

        public static void ResetHotPaths()
        {
            _hotPaths.Clear();
            _hotPathsSorted.Clear();
            _hotPathSortDirty = 1;
        }

        private static float[] GetOrderedHistory( float[] ring )
        {
            float[] ordered = new float[_historyCount];
            if( _historyCount < HistorySize )
                Array.Copy( ring, 0, ordered, 0, _historyCount );
            else
            {
                int start = _historyIndex;
                int tail = HistorySize - start;
                Array.Copy( ring, start, ordered, 0,    tail );
                Array.Copy( ring, 0,     ordered, tail, start );
            }

            return ordered;
        }

        private static double LastMs => LastCpu * 1000.0 / Stopwatch.Frequency;
        private static double PeakMs => PeakCpu * 1000.0 / Stopwatch.Frequency;

        private static bool HasAnyBytesData() => _hotPaths.Values.Any( entry => entry.HasNetworkData );

        public static void DrawImGuiPopout()
        {
            if( !_poppedOut )
                return;

            if( !ImGui.Begin( "Profiler", ref _poppedOut ) )
            {
                ImGui.End();
                return;
            }

            DrawControls();
            ImGui.Separator();
            DrawContent();

            ImGui.End();
        }

        public static void DrawImGuiInline()
        {
            DrawControls();
            ImGui.Separator();
            DrawContent();
        }

        private static void DrawControls()
        {
            ImGui.Checkbox( "Enabled##Profiler", ref Enabled );

            ImGui.SameLine();
            if( ImGui.Button( "Reset##Profiler" ) )
                Reset();

            ImGui.SameLine();
            if( ImGui.Button( "Reset Hot Paths##Profiler" ) )
                ResetHotPaths();

            ImGui.SameLine();
            if( ImGui.Button( _poppedOut ? "Dock##Profiler" : "Pop Out##Profiler" ) )
                _poppedOut = !_poppedOut;
        }

        private static void DrawContent()
        {
            bool  hasNetwork = TotalPolls > 0;
            float pollMs = ( float )LastMs;

            if( hasNetwork )
            {
                ImGui.TextColored(
                    pollMs > 2.0f ? new UnityEngine.Vector4( 1f,   0.3f, 0.3f, 1f ) :
                    pollMs > 0.5f ? new UnityEngine.Vector4( 1f,   1f,   0.3f, 1f ) :
                                    new UnityEngine.Vector4( 0.3f, 1f,   0.3f, 1f ),
                    $"{pollMs:F3}ms  |  {LastMessage} msgs  |  {FormatBytes( LastNetwork )}" );
            }
            else
                ImGui.TextColored( new UnityEngine.Vector4( 0.3f, 1f, 0.3f, 1f ), "Profiler" );

            if( hasNetwork )
            {
                if( ImGui.CollapsingHeader( $"Graphs##Profiler", ref _showGraphs, ImGuiTreeNodeFlags.DefaultOpen ) )
                {
                    if( _historyCount == 0 )
                        ImGui.TextDisabled( "No data yet..." );
                    else
                    {
                        float       graphWidth = ImGui.GetContentRegionAvail().x;
                        const float graphHeight = 60f;

                        float[] msData = GetOrderedHistory( _cpuHistory );
                        ImGui.Text( $"Processing Time (ms)  [avg: {AvgCpu:F3}ms  peak: {PeakMs:F3}ms]" );
                        ImGui.PlotLines( $"##ms_Profiler", ref msData[ 0 ], msData.Length, 0, null, 0f, _cpuGraphMax * 1.2f, new UnityEngine.Vector2( graphWidth, graphHeight ) );

                        float[] bytesData = GetOrderedHistory( _networkHistory );
                        ImGui.Text( $"Bytes/Poll  [avg: {FormatBytes( ( long )AvgNetwork )}  peak: {FormatBytes( PeakNetwork )}]" );
                        ImGui.PlotLines( $"##bytes_Profiler", ref bytesData[ 0 ], bytesData.Length, 0, null, 0f, _networkGraphMax * 1.2f, new UnityEngine.Vector2( graphWidth, graphHeight ) );

                        float[] msgData = GetOrderedHistory( _messageHistory );
                        ImGui.Text( $"Messages/Poll  [avg: {AvgMessage:F1}  peak: {PeakMessages}]" );
                        ImGui.PlotHistogram( $"##msgs_Profiler", ref msgData[ 0 ], msgData.Length, 0, null, 0f, _messageGraphMax * 1.2f, new UnityEngine.Vector2( graphWidth, graphHeight ) );
                    }
                }
            }

            DrawHotPathsSection();
        }

        private static void DrawHotPathsSection()
        {
            if( !ImGui.CollapsingHeader( $"Hot Paths##Profiler", ref _showHotPaths, ImGuiTreeNodeFlags.DefaultOpen ) )
                return;

            if( _hotPaths.Count == 0 )
            {
                ImGui.TextDisabled( "No callsite data recorded yet." );
                return;
            }

            ImGui.InputText( $"Filter##Profiler_hp", ref _hotPathFilter, 128 );

            _hotPathFrameAccum++;
            if( _hotPathSortDirty != 0 || _hotPathFrameAccum >= HotPathSortInterval )
            {
                _hotPathFrameAccum = 0;
                _hotPathSortDirty = 0;
                RebuildSortedHotPaths();
            }

            bool showBytesCols = HasAnyBytesData();
            int  colCount = showBytesCols ? 8 : 5;

            const ImGuiTableFlags tableFlags =
                ImGuiTableFlags.Borders        |
                ImGuiTableFlags.RowBg          |
                ImGuiTableFlags.ScrollY        |
                ImGuiTableFlags.Sortable       |
                ImGuiTableFlags.SizingFixedFit |
                ImGuiTableFlags.Resizable;

            float tableHeight = Math.Min( 400f, 24f + _hotPathsSorted.Count * 22f );

            if( !ImGui.BeginTable( $"hotpaths_Profiler", colCount, tableFlags, new UnityEngine.Vector2( 0, tableHeight ) ) )
                return;

            ImGui.TableSetupColumn( "Callsite",   ImGuiTableColumnFlags.DefaultSort          | ImGuiTableColumnFlags.WidthStretch,                                   0f, 0 );
            ImGui.TableSetupColumn( "Calls",      ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed,                                     0f, 1 );
            ImGui.TableSetupColumn( "Avg (ms)",   ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 0f, 2 );
            ImGui.TableSetupColumn( "Peak (ms)",  ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed,                                     0f, 3 );
            ImGui.TableSetupColumn( "Total (ms)", ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed,                                     0f, 4 );

            if( showBytesCols )
            {
                ImGui.TableSetupColumn( "Avg Bytes",   ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 0f, 5 );
                ImGui.TableSetupColumn( "Peak Bytes",  ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 0f, 6 );
                ImGui.TableSetupColumn( "Total Bytes", ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthFixed, 0f, 7 );
            }

            ImGui.TableHeadersRow();

            var sortSpecs = ImGui.TableGetSortSpecs();

            if( sortSpecs.SpecsDirty )
            {
                if( sortSpecs.SpecsCount > 0 )
                {
                    var spec = sortSpecs.Specs[ 0 ];
                    _hotPathSortColumn = ( int )spec.ColumnUserID;
                    _hotPathSortAscending = spec.SortDirection == ImGuiSortDirection.Ascending;
                }

                _hotPathSortDirty = 1;
                sortSpecs.SpecsDirty = false;
            }

            bool hasFilter = !string.IsNullOrEmpty( _hotPathFilter );

            foreach( var entry in _hotPathsSorted )
            {
                if( hasFilter )
                {
                    bool matchesKey = entry.Key.IndexOf( _hotPathFilter, StringComparison.OrdinalIgnoreCase )      >= 0;
                    bool matchesFile = entry.FilePath.IndexOf( _hotPathFilter, StringComparison.OrdinalIgnoreCase ) >= 0;
                    if( !matchesKey && !matchesFile )
                        continue;
                }

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex( 0 );
                string? shortFile = string.IsNullOrEmpty( entry.FilePath ) ? null : Path.GetFileName( entry.FilePath );
                string? location = shortFile != null && entry.LineNumber > 0 ? $"{shortFile}:{entry.LineNumber}" : shortFile;
                string  displayKey = entry.Key;
                if( entry.LineNumber > 0 )
                    displayKey = displayKey.Replace( $":{entry.LineNumber}", "" );

                string displayName = location != null ? $"{displayKey}  [{location}]" : displayKey;

                if( entry.AvgCpu > 1.0 )
                    ImGui.TextColored( new UnityEngine.Vector4( 1f, 0.3f, 0.3f, 1f ), displayName );
                else if( entry.AvgCpu > 0.2 )
                    ImGui.TextColored( new UnityEngine.Vector4( 1f, 1f, 0.3f, 1f ), displayName );
                else
                    ImGui.Text( displayName );

                if( ImGui.IsItemHovered() && !string.IsNullOrEmpty( entry.FilePath ) )
                {
                    ImGui.BeginTooltip();
                    ImGui.Text( $"{entry.FilePath}" );
                    ImGui.Text( $"{entry.MemberName}() : line {entry.LineNumber}" );
                    ImGui.Text( $"Avg: {entry.AvgCpu:F4}ms  Peak: {entry.PeakCpu:F4}ms  Calls: {entry.Calls:N0}" );
                    if( entry.HasNetworkData )
                        ImGui.Text( $"Avg Bytes: {FormatBytes( ( long )entry.AvgNetwork )}  Peak: {FormatBytes( entry.PeakNetwork )}  Total: {FormatBytes( entry.TotalNetwork )}" );
                    ImGui.EndTooltip();
                }

                ImGui.TableSetColumnIndex( 1 );
                ImGui.Text( $"{entry.Calls:N0}" );

                ImGui.TableSetColumnIndex( 2 );
                ImGui.Text( $"{entry.AvgCpu:F4}" );

                ImGui.TableSetColumnIndex( 3 );
                ImGui.Text( $"{entry.PeakCpu:F4}" );

                ImGui.TableSetColumnIndex( 4 );
                ImGui.Text( $"{entry.TotalCpu:F2}" );

                if( !showBytesCols )
                    continue;

                ImGui.TableSetColumnIndex( 5 );
                ImGui.Text( entry.HasNetworkData ? FormatBytes( ( long )entry.AvgNetwork ) : "-" );

                ImGui.TableSetColumnIndex( 6 );
                ImGui.Text( entry.HasNetworkData ? FormatBytes( entry.PeakNetwork ) : "-" );

                ImGui.TableSetColumnIndex( 7 );
                ImGui.Text( entry.HasNetworkData ? FormatBytes( entry.TotalNetwork ) : "-" );
            }

            ImGui.EndTable();
        }

        private static void RebuildSortedHotPaths()
        {
            _hotPathsSorted = new List< HotPathEntry >( _hotPaths.Values );

            switch( _hotPathSortColumn )
            {
                case 0:
                    _hotPathsSorted.Sort( ( a, b ) =>
                        _hotPathSortAscending ? string.Compare( a.Key, b.Key, StringComparison.OrdinalIgnoreCase ) : string.Compare( b.Key, a.Key, StringComparison.OrdinalIgnoreCase ) );
                    break;
                case 1:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.Calls.CompareTo( b.Calls ) : b.Calls.CompareTo( a.Calls ) );
                    break;
                case 2:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.AvgCpu.CompareTo( b.AvgCpu ) : b.AvgCpu.CompareTo( a.AvgCpu ) );
                    break;
                case 3:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.PeakCpu.CompareTo( b.PeakCpu ) : b.PeakCpu.CompareTo( a.PeakCpu ) );
                    break;
                case 4:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.TotalCpu.CompareTo( b.TotalCpu ) : b.TotalCpu.CompareTo( a.TotalCpu ) );
                    break;
                case 5:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.AvgNetwork.CompareTo( b.AvgNetwork ) : b.AvgNetwork.CompareTo( a.AvgNetwork ) );
                    break;
                case 6:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.PeakNetwork.CompareTo( b.PeakNetwork ) : b.PeakNetwork.CompareTo( a.PeakNetwork ) );
                    break;
                case 7:
                    _hotPathsSorted.Sort( ( a, b ) => _hotPathSortAscending ? a.TotalNetwork.CompareTo( b.TotalNetwork ) : b.TotalNetwork.CompareTo( a.TotalNetwork ) );
                    break;
            }
        }

        private static string FormatBytes( long bytes )
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double   len = bytes;
            int      order = 0;
            while( len >= 1024 && order < sizes.Length - 1 )
            {
                order++;
                len /= 1024;
            }

            return $"{len:0.##} {sizes[ order ]}";
        }
    }
}
#else
namespace Shared.Profiling
{
    public static class Profiler
    {
        public struct ProfileScope : IDisposable
        {
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            internal ProfileScope( string? key, string memberName, string filePath, int lineNumber )
            {
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            private void End()
            {
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            private string ResolveKey( string? extra = null ) => "";

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string key )
            {
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string key, int bytes )
            {
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( int messageCount, int bytes ) => End( null, messageCount, bytes );

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void End( string? key, int messageCount, int bytes )
            {
            }

            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Dispose() => End();
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static ProfileScope Scope( [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0 ) => default;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static ProfileScope Scope( string key, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0 ) => default;
    }
}
#endif