using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ONI_MP.Misc;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;

namespace ONI_MP.DebugTools
{
    public class PacketTracker
    {
        private static PacketTracker _instance;
        private bool showWindow = false;

        private string outgoing_filter = string.Empty;
        private string incoming_filter = string.Empty;

        // Used for imgui packet tracking
        public struct PacketTrackData
        {
            public IPacket packet;
            public int size;
        }

        private List<PacketTrackData> incoming_tracked = new List<PacketTrackData>();
        private List<PacketTrackData> outgoing_tracked = new List<PacketTrackData>();
        private const int MAX_TRACKED_LIMIT = 100;

        public static PacketTracker Init()
        {
            if (_instance != null)
                return _instance;

            _instance = new PacketTracker();
            return _instance;
        }

        public static void TrackSent(PacketTrackData data)
        {
            _instance.outgoing_tracked.Add(data);

            if (_instance.outgoing_tracked.Count > MAX_TRACKED_LIMIT)
            {
                int overflow = _instance.outgoing_tracked.Count - MAX_TRACKED_LIMIT;
                _instance.outgoing_tracked.RemoveRange(0, overflow);
            }
        }

        public static void TrackIncoming(PacketTrackData data)
        {
            _instance.incoming_tracked.Add(data);

            if (_instance.incoming_tracked.Count > MAX_TRACKED_LIMIT)
            {
                int overflow = _instance.incoming_tracked.Count - MAX_TRACKED_LIMIT;
                _instance.incoming_tracked.RemoveRange(0, overflow);
            }
        }

        public void Clear()
        {
            _instance.outgoing_tracked.Clear();
            _instance.incoming_tracked.Clear();
        }

        public void Toggle()
        {
            showWindow = !showWindow;
        }

        public void ShowWindow()
        {
            if (!showWindow)
                return;

            if (ImGui.Begin("Packet Tracker", ref showWindow))
            {
                if (!MultiplayerSession.InSession)
                {
                    if (outgoing_tracked.Count > 0)
                        Clear();

                    ImGui.TextDisabled("Not in a session!");
                }
                else
                {
                    if (ImGui.CollapsingHeader("Incoming Packets"))
                    {
                        ImGui.InputText("Filter", ref incoming_filter, 64);
                        ImGui.Separator();

                        AddTable("incoming_packets_table", incoming_tracked, incoming_filter);
                    }

                    if (ImGui.CollapsingHeader("Outgoing Packets"))
                    {
                        ImGui.InputText("Filter", ref outgoing_filter, 64);
                        ImGui.Separator();

                        AddTable("outgoing_packets_table", outgoing_tracked, outgoing_filter);
                    }
                }
            }

            ImGui.End();
        }

        public void ShowInTab()
        {
            if (!MultiplayerSession.InSession)
            {
                if (outgoing_tracked.Count > 0)
                    Clear();

                ImGui.TextDisabled("Not in a session!");
                return;
            }

            if (ImGui.CollapsingHeader("Incoming Packets", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.InputText("Filter##Incoming", ref incoming_filter, 64);
                ImGui.Separator();

                AddTable(
                    "incoming_packets_table_tab",
                    incoming_tracked,
                    incoming_filter
                );
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Outgoing Packets", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.InputText("Filter##Outgoing", ref outgoing_filter, 64);
                ImGui.Separator();

                AddTable(
                    "outgoing_packets_table_tab",
                    outgoing_tracked,
                    outgoing_filter
                );
            }
        }

        private void AddTable(string str_id, List<PacketTrackData> dataset, string filter)
        {
            if (ImGui.BeginTable(str_id, 3,
                        ImGuiTableFlags.Borders |
                        ImGuiTableFlags.RowBg |
                        ImGuiTableFlags.ScrollY, new UnityEngine.Vector2(0, 400)))
            {
                ImGui.TableSetupColumn("Packet Type");
                ImGui.TableSetupColumn("Packet ID");
                ImGui.TableSetupColumn("Size (bytes)");

                ImGui.TableHeadersRow();

                for (int i = dataset.Count - 1; i >= 0; i--)
                {
                    var entry = dataset[i];

                    string typeName = entry.packet.GetType().Name;
                    string idString = entry.packet.GetType().GetHashCode().ToString();

                    if (!string.IsNullOrEmpty(filter))
                    {
                        bool matchesType =
                            typeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                        bool matchesId =
                            idString.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                        if (!matchesType && !matchesId)
                            continue;
                    }

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(typeName);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(idString);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(Utils.FormatBytes(entry.size));
                }

                ImGui.EndTable();
            }
        }

    }
}
