using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Clear;
using ONI_MP.Networking;
using UnityEngine;
using STRINGS;

namespace ONI_MP.Patches.ToolPatches.Mop
{
    [HarmonyPatch(typeof(MopTool), "OnDragTool")]
    public static class MoptoolPatch
    {
        public static bool Prefix(int cell, int distFromOrigin)
        {
            TryMop(cell);
            return false;
        }

        public static void TryMop(int cell)
        {
            if (!Grid.IsValidCell(cell))
                return;

            if (DebugHandler.InstantBuildMode)
            {
                Moppable.MopCell(cell, 1000000f, null);
                return;
            }

            GameObject gameObject = Grid.Objects[cell, 8];
            if (!Grid.Solid[cell] && gameObject == null && Grid.Element[cell].IsLiquid)
            {
                bool onFloor = Grid.IsValidCell(Grid.CellBelow(cell)) && Grid.Solid[Grid.CellBelow(cell)];
                bool underLimit = Grid.Mass[cell] <= MopTool.maxMopAmt;

                if (onFloor && underLimit)
                {
                    gameObject = (Grid.Objects[cell, 8] = Util.KInstantiate(Assets.GetPrefab(new Tag("MopPlacer"))));
                    Vector3 position = Grid.CellToPosCBC(cell, MopTool.Instance.visualizerLayer);
                    position.z -= 0.15f;
                    gameObject.transform.SetPosition(position);
                    gameObject.SetActive(true);

                    var prioritizable = gameObject.GetComponent<Prioritizable>();
                    if (prioritizable != null)
                        prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());

                    if (MultiplayerSession.InSession)
                    {
                        var packet = new ClearPacket
                        {
                            SenderId = MultiplayerSession.LocalSteamID,
                            TargetCells = new List<int> { cell },
                            ActionType = ClearActionType.Mop
                        };

                        if (MultiplayerSession.IsHost)
                        {
                            PacketSender.SendToAllClients(packet);
                        }
                        else
                        {
                            PacketSender.SendToHost(packet);
                        }
                    }
                }
                else
                {
                    string message = !onFloor
                        ? Strings.Get("STRINGS.UI.TOOLS.MOP.NOT_ON_FLOOR")
                        : Strings.Get("STRINGS.UI.TOOLS.MOP.TOO_MUCH_LIQUID");
                    PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative, message, null, Grid.CellToPosCBC(cell, MopTool.Instance.visualizerLayer));
                }
            }

            // In case a mop placer already exists
            if (gameObject != null)
            {
                var prioritizable = gameObject.GetComponent<Prioritizable>();
                if (prioritizable != null)
                    prioritizable.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
            }
        }
    }

}
