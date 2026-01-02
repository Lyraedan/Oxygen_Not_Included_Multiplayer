using ONI_MP.DebugTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.World.Handlers
{
    internal class BuildingEnableHandler : IBuildingConfigHandler
    {

        private static readonly int[] _hashes = new int[]
        {
            "QueueBuildingEnableStateChange".GetHashCode(),
            "BuildingEnableStateChange".GetHashCode(),
        };

        public int[] SupportedConfigHashes => _hashes;

        public bool TryApplyConfig(GameObject go, BuildingConfigPacket packet)
        {
            BuildingEnabledButton enabledButton = go.GetComponent<BuildingEnabledButton>();
            if (enabledButton == null) return false;

            DebugConsole.Log($"[BuildingEnableHandler] Handling Enable State Change on {go.name}");

            bool targetState = packet.Value > 0.5f;

            if (packet.ConfigHash == "QueueBuildingEnableStateChange".GetHashCode())
            {
                DebugConsole.Log($"[BuildingEnableHandler] Queue Building Enabled Change current={enabledButton.queuedToggle} new={targetState}");

                // Check if we are already queued
                if (targetState != enabledButton.queuedToggle) enabledButton.OnMenuToggle();
                return true;
            }
            else if (packet.ConfigHash == "BuildingEnableStateChange".GetHashCode())
            {
                DebugConsole.Log($"[BuildingEnableHandler] Changing Building Enabled State current={enabledButton.IsEnabled} new={targetState}");

                // Check if we are already toggled
                if (targetState != enabledButton.IsEnabled) enabledButton.HandleToggle();
                return true;
            }

            return false;
        }
    }
}
