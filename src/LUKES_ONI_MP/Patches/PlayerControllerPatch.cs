using HarmonyLib;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.States;
using UnityEngine;
using static STRINGS.INPUT_BINDINGS;

namespace ONI_MP.Patches
{
    [HarmonyPatch]
    public static class PlayerControllerPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "ActivateTool")]
        public static void ActivateTool_Postfix(InterfaceTool tool)
        {
            if (tool == null)
            {
                CursorManager.Instance.cursorState = CursorState.NONE;
                return;
            }

            switch (tool.GetType().Name)
            {
                case "SelectTool":
                    CursorManager.Instance.cursorState = CursorState.SELECT;
                    break;
                case "BuildTool":
                    CursorManager.Instance.cursorState = CursorState.BUILD;
                    break;
                case "DigTool":
                    CursorManager.Instance.cursorState = CursorState.DIG;
                    break;
                case "CancelTool":
                    CursorManager.Instance.cursorState = CursorState.CANCEL;
                    break;
                case "DeconstructTool":
                    CursorManager.Instance.cursorState = CursorState.DECONSTRUCT;
                    break;
                case "PrioritizeTool":
                {
                    var priorityTool = PlayerController.Instance?.ActiveTool as PrioritizeTool;
                    if (priorityTool != null)
                    {
                        var priority = ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority();

                        if (priority.priority_value >= 5)
                        {
                            CursorManager.Instance.cursorState = CursorState.PRIORITIZE;
                        }
                        else
                        {
                            CursorManager.Instance.cursorState = CursorState.DEPRIORITIZE;
                        }
                    }
                    else
                    {
                        CursorManager.Instance.cursorState = CursorState.PRIORITIZE; // Fallback to prioritize
                    }
                    break;
                }
                case "ClearTool":
                    CursorManager.Instance.cursorState = CursorState.SWEEP;
                    break;
                case "MopTool":
                    CursorManager.Instance.cursorState = CursorState.MOP;
                    break;
                case "HarvestTool":
                    CursorManager.Instance.cursorState = CursorState.HARVEST;
                    break;
                case "DisinfectTool":
                    CursorManager.Instance.cursorState = CursorState.DISINFECT;
                    break;
                case "AttackTool":
                    CursorManager.Instance.cursorState = CursorState.ATTACK;
                    break;
                case "CaptureTool":
                    CursorManager.Instance.cursorState = CursorState.CAPTURE;
                    break;
                case "WrangleTool":
                    CursorManager.Instance.cursorState = CursorState.WRANGLE;
                    break;
                case "EmptyPipeTool":
                    CursorManager.Instance.cursorState = CursorState.EMPTY_PIPE;
                    break;
                case "ClearFloorTool":
                    CursorManager.Instance.cursorState = CursorState.CLEAR_FLOOR;
                    break;
                case "MoveToTool":
                    CursorManager.Instance.cursorState = CursorState.MOVE_TO;
                    break;
                case "DisconnectTool":
                    CursorManager.Instance.cursorState = CursorState.DISCONNECT;
                    break;
                default:
                    CursorManager.Instance.cursorState = CursorState.NONE;
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerController), "DeactivateTool")]
        public static void DeactivateTool_Postfix()
        {
            CursorManager.Instance.cursorState = CursorState.NONE;
        }
    }
}
