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
            "BuildingEnableState".GetHashCode(),
        };

        public int[] SupportedConfigHashes => _hashes;

        public bool TryApplyConfig(GameObject go, BuildingConfigPacket packet)
        {
            if (packet.ConfigHash != "BuildingEnableState".GetHashCode()) return false;

            BuildingEnabledButton enabledButton = go.GetComponent<BuildingEnabledButton>();
            if (enabledButton == null) return false;

            bool targetQueueToggleState = packet.Value > 0.5f;
            if (targetQueueToggleState != enabledButton.queuedToggle) enabledButton.OnMenuToggle();
            return true;
        }
    }
}
