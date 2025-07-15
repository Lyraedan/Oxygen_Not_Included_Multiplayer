using HarmonyLib;
using ONI_MP.Networking;
using ONI_MP.Networking.Packets.Architecture;
using ONI_MP.Networking.Packets.Tools.Disinfect;
using UnityEngine;

[HarmonyPatch(typeof(DisinfectTool), "OnDragTool")]
public class DisinfectToolPatch
{
    [HarmonyPrefix]
    public static bool Prefix(int cell, int distFromOrigin)
    {
        MarkForDisinfect(cell);
        return false;
    }

    private static void MarkForDisinfect(int cell)
    {
        for (int i = 0; i < 45; i++)
        {
            GameObject gameObject = Grid.Objects[cell, i];
            if (gameObject != null)
            {
                Disinfectable component = gameObject.GetComponent<Disinfectable>();
                if (component != null && component.GetComponent<PrimaryElement>().DiseaseCount > 0)
                {
                    component.MarkForDisinfect();

                    var packet = new DisinfectPacket()
                    {
                        Cell = cell
                    };

                    if(MultiplayerSession.IsHost)
                    {
                        PacketSender.SendToAllClients(packet);
                    } 
                    else
                    {
                        PacketSender.SendToHost(packet);
                    }
                }
            }
        }
    }
}
