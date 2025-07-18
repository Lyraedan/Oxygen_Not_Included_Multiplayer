using System;
using System.Collections.Generic;
using System.IO;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using ONI_MP.Networking.Packets.Architecture;
using UnityEngine;
using Klei.AI;

namespace ONI_MP.Networking.Packets.DuplicantBehavior
{
    /// <summary>
    /// Enhanced pathfinding coordination beyond the basic NavigatorPath packet.
    /// Synchronizes pathfinding states, destination changes, and movement coordination.
    /// </summary>
    public class PathfindingUpdatePacket : IPacket
    {
        public int DuplicantNetId;
        public string PathfindingState;     // "PathCalculating", "PathFound", "PathBlocked", "Rerouting", "DestinationReached"
        public Vector3 CurrentDestination;  // Current target destination
        public Vector3 IntermediateTarget; // Next immediate waypoint
        public int DestinationCell;         // Cell of the final destination
        public string MovementType;         // "Walking", "Climbing", "Swimming", "Flying", "Tube"
        public float MovementSpeed;         // Current movement speed
        public bool IsStuck;                // Is the duplicant stuck/blocked?
        public string BlockingReason;       // "Other Duplicant", "Obstacle", "Unreachable", "Door Locked"
        public List<int> AlternatePath;     // Alternative path cells if main route blocked
        public float PathingPriority;       // Priority for pathfinding conflicts
        public bool IsEmergencyNavigation;  // Emergency movement (fire, suffocation, etc.)
        public System.DateTime PathStartTime;

        public PacketType Type => PacketType.PathfindingUpdate;

        public PathfindingUpdatePacket()
        {
            AlternatePath = new List<int>();
            PathStartTime = System.DateTime.UtcNow;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(DuplicantNetId);
            writer.Write(PathfindingState ?? "");
            
            writer.Write(CurrentDestination.x);
            writer.Write(CurrentDestination.y);
            writer.Write(CurrentDestination.z);
            
            writer.Write(IntermediateTarget.x);
            writer.Write(IntermediateTarget.y);
            writer.Write(IntermediateTarget.z);
            
            writer.Write(DestinationCell);
            writer.Write(MovementType ?? "");
            writer.Write(MovementSpeed);
            writer.Write(IsStuck);
            writer.Write(BlockingReason ?? "");
            
            writer.Write(AlternatePath.Count);
            foreach (var cell in AlternatePath)
            {
                writer.Write(cell);
            }
            
            writer.Write(PathingPriority);
            writer.Write(IsEmergencyNavigation);
            writer.Write(PathStartTime.ToBinary());
        }

        public void Deserialize(BinaryReader reader)
        {
            DuplicantNetId = reader.ReadInt32();
            PathfindingState = reader.ReadString();
            
            CurrentDestination = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            IntermediateTarget = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
            
            DestinationCell = reader.ReadInt32();
            MovementType = reader.ReadString();
            MovementSpeed = reader.ReadSingle();
            IsStuck = reader.ReadBoolean();
            BlockingReason = reader.ReadString();
            
            AlternatePath.Clear();
            int pathCount = reader.ReadInt32();
            for (int i = 0; i < pathCount; i++)
            {
                AlternatePath.Add(reader.ReadInt32());
            }
            
            PathingPriority = reader.ReadSingle();
            IsEmergencyNavigation = reader.ReadBoolean();
            PathStartTime = System.DateTime.FromBinary(reader.ReadInt64());
        }

        public void OnDispatched()
        {
            if (!NetworkIdentityRegistry.TryGet(DuplicantNetId, out var entity))
            {
                DebugConsole.LogWarning($"[PathfindingUpdatePacket] Could not find duplicant with NetId {DuplicantNetId}");
                return;
            }

            var duplicantGO = entity.gameObject;
            var navigator = duplicantGO.GetComponent<Navigator>();
            var choreDriver = duplicantGO.GetComponent<ChoreDriver>();

            if (navigator == null)
            {
                DebugConsole.LogWarning($"[PathfindingUpdatePacket] Duplicant {DuplicantNetId} missing Navigator component");
                return;
            }

            try
            {
                // Handle different pathfinding states
                switch (PathfindingState)
                {
                    case "PathCalculating":
                        HandlePathCalculatingState(duplicantGO, navigator);
                        break;
                        
                    case "PathFound":
                        HandlePathFoundState(duplicantGO, navigator);
                        break;
                        
                    case "PathBlocked":
                        HandlePathBlockedState(duplicantGO, navigator, choreDriver);
                        break;
                        
                    case "Rerouting":
                        HandleReroutingState(duplicantGO, navigator);
                        break;
                        
                    case "DestinationReached":
                        HandleDestinationReachedState(duplicantGO, navigator, choreDriver);
                        break;
                        
                    default:
                        DebugConsole.LogWarning($"[PathfindingUpdatePacket] Unknown pathfinding state: {PathfindingState}");
                        break;
                }

                // Update movement speed
                UpdateMovementSpeed(duplicantGO);

                // Handle emergency navigation
                if (IsEmergencyNavigation)
                {
                    HandleEmergencyNavigation(duplicantGO, navigator, choreDriver);
                }

                DebugConsole.Log($"[PathfindingUpdatePacket] Applied {PathfindingState} to duplicant {DuplicantNetId} " +
                               $"(Destination: {CurrentDestination}, Speed: {MovementSpeed:F1})");
            }
            catch (Exception ex)
            {
                DebugConsole.LogException(ex);
            }
        }

        private void HandlePathCalculatingState(GameObject duplicantGO, Navigator navigator)
        {
            // Show that pathfinding is in progress
            // For clients, this could display a "thinking" indicator
            if (navigator.IsMoving())
            {
                navigator.Stop(); // Stop current movement while calculating
            }
        }

        private void HandlePathFoundState(GameObject duplicantGO, Navigator navigator)
        {
            // Path has been calculated, start movement
            if (Grid.IsValidCell(DestinationCell))
            {
                // Create target cell for navigation
                var targetCell = Grid.PosToCell(CurrentDestination);
                
                // Clean up callback
                System.Action cleanup = () => {
                    // Navigation completed or failed
                    DebugConsole.Log($"[PathfindingUpdatePacket] Navigation completed for duplicant {DuplicantNetId}");
                };
                
                navigator.Subscribe((int)GameHashes.DestinationReached, (data) => cleanup.Invoke());
                navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => cleanup.Invoke());
                
                // Start navigation
                navigator.GoTo(targetCell, new CellOffset[] { CellOffset.none });
            }
        }

        private void HandlePathBlockedState(GameObject duplicantGO, Navigator navigator, ChoreDriver choreDriver)
        {
            // Path is blocked, handle based on blocking reason
            switch (BlockingReason)
            {
                case "Other Duplicant":
                    HandleDuplicantBlocking(duplicantGO, navigator);
                    break;
                    
                case "Obstacle":
                    HandleObstacleBlocking(duplicantGO, navigator);
                    break;
                    
                case "Unreachable":
                    HandleUnreachableDestination(duplicantGO, navigator, choreDriver);
                    break;
                    
                case "Door Locked":
                    HandleLockedDoorBlocking(duplicantGO, navigator);
                    break;
                    
                default:
                    // Generic blocking - try to wait or reroute
                    TryAlternatePath(duplicantGO, navigator);
                    break;
            }
        }

        private void HandleReroutingState(GameObject duplicantGO, Navigator navigator)
        {
            // Use alternate path if available
            if (AlternatePath.Count > 0)
            {
                TryAlternatePath(duplicantGO, navigator);
            }
            else
            {
                // No alternate path, recalculate from scratch
                HandlePathCalculatingState(duplicantGO, navigator);
            }
        }

        private void HandleDestinationReachedState(GameObject duplicantGO, Navigator navigator, ChoreDriver choreDriver)
        {
            // Destination reached, stop movement
            if (navigator.IsMoving())
            {
                navigator.Stop();
            }

            // Notify chore system that destination was reached
            var currentChore = choreDriver?.GetCurrentChore();
            if (currentChore != null)
            {
                // Trigger destination reached event for chore
                duplicantGO.Trigger((int)GameHashes.DestinationReached);
            }
        }

        private void HandleDuplicantBlocking(GameObject duplicantGO, Navigator navigator)
        {
            // Wait briefly for other duplicant to move, then try alternate path
            // In a real implementation, this might involve coordination between duplicants
            TryAlternatePath(duplicantGO, navigator);
        }

        private void HandleObstacleBlocking(GameObject duplicantGO, Navigator navigator)
        {
            // Try alternate path or wait for obstacle to be removed
            if (AlternatePath.Count > 0)
            {
                TryAlternatePath(duplicantGO, navigator);
            }
            else
            {
                // Wait and retry later
                navigator.Stop();
            }
        }

        private void HandleUnreachableDestination(GameObject duplicantGO, Navigator navigator, ChoreDriver choreDriver)
        {
            // Destination is unreachable, cancel current task
            navigator.Stop();
            
            var currentChore = choreDriver?.GetCurrentChore();
            if (currentChore != null)
            {
                currentChore.Cancel("Destination unreachable");
                DebugConsole.LogWarning($"[PathfindingUpdatePacket] Cancelled chore for {DuplicantNetId} - destination unreachable");
            }
        }

        private void HandleLockedDoorBlocking(GameObject duplicantGO, Navigator navigator)
        {
            // Try to find alternate route around locked door
            if (AlternatePath.Count > 0)
            {
                TryAlternatePath(duplicantGO, navigator);
            }
            else
            {
                // No alternate route, wait or cancel task
                navigator.Stop();
            }
        }

        private void TryAlternatePath(GameObject duplicantGO, Navigator navigator)
        {
            if (AlternatePath.Count == 0) return;

            // Create new path from alternate cells
            var newPath = new PathFinder.Path
            {
                nodes = new List<PathFinder.Path.Node>()
            };

            foreach (var cell in AlternatePath)
            {
                if (Grid.IsValidCell(cell))
                {
                    newPath.nodes.Add(new PathFinder.Path.Node
                    {
                        cell = cell,
                        navType = NavType.Floor, // Default to floor navigation
                        transitionId = 0
                    });
                }
            }

            if (newPath.nodes.Count > 0)
            {
                navigator.path = newPath;
                
                // Create target for final destination
                var finalCell = AlternatePath[AlternatePath.Count - 1];
                var finalPos = Grid.CellToPosCBC(finalCell, Grid.SceneLayer.Move);
                
                var targetCell = Grid.PosToCell(finalPos);
                
                // Clean up callback
                System.Action cleanup = () => {
                    // Navigation completed or failed
                    DebugConsole.Log($"[PathfindingUpdatePacket] Alternate path navigation completed for duplicant {DuplicantNetId}");
                };
                
                navigator.Subscribe((int)GameHashes.DestinationReached, (data) => cleanup.Invoke());
                navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => cleanup.Invoke());
                
                // Start alternate path navigation
                navigator.GoTo(targetCell, new CellOffset[] { CellOffset.none });
                
                DebugConsole.Log($"[PathfindingUpdatePacket] Using alternate path with {AlternatePath.Count} waypoints for {DuplicantNetId}");
            }
        }

        private void UpdateMovementSpeed(GameObject duplicantGO)
        {
            // TODO: Implement proper movement speed modification
            // Update duplicant movement speed based on movement type and conditions
            /*var attributeModifiers = duplicantGO.GetComponent<Klei.AI.AttributeModifiers>();
            if (attributeModifiers != null)
            {
                var speedModifier = new Klei.AI.AttributeModifier(
                    Db.Get().Attributes.Athletics.Id,
                    MovementSpeed - 1.0f, // Adjust relative to base speed
                    "Pathfinding Speed",
                    false,
                    false,
                    true
                );
                
                attributeModifiers.Add(speedModifier);
            }*/

            // Apply movement type effects
            var effects = duplicantGO.GetComponent<Klei.AI.Effects>();
            if (effects != null)
            {
                switch (MovementType)
                {
                    case "Swimming":
                        effects.Add("Swimming", true);
                        break;
                    case "Climbing":
                        effects.Add("Climbing", true);
                        break;
                    case "Flying":
                        effects.Add("Flying", true);
                        break;
                }
            }
        }

        private void HandleEmergencyNavigation(GameObject duplicantGO, Navigator navigator, ChoreDriver choreDriver)
        {
            // Emergency navigation takes priority over all other tasks
            var currentChore = choreDriver?.GetCurrentChore();
            if (currentChore != null && !IsEmergencyChore(currentChore.choreType.Id))
            {
                currentChore.Cancel("Emergency navigation required");
            }

            // Force high priority navigation
            if (Grid.IsValidCell(DestinationCell))
            {
                var targetCell = Grid.PosToCell(CurrentDestination);
                
                // Clean up callback
                System.Action cleanup = () => {
                    // Emergency navigation completed or failed
                    DebugConsole.Log($"[PathfindingUpdatePacket] Emergency navigation completed for duplicant {DuplicantNetId}");
                };
                
                navigator.Subscribe((int)GameHashes.DestinationReached, (data) => cleanup.Invoke());
                navigator.Subscribe((int)GameHashes.NavigationFailed, (data) => cleanup.Invoke());
                
                // Force immediate navigation
                navigator.GoTo(targetCell, new CellOffset[] { CellOffset.none });
                
                DebugConsole.Log($"[PathfindingUpdatePacket] Emergency navigation activated for {DuplicantNetId}");
            }
        }

        private bool IsEmergencyChore(string choreTypeId)
        {
            return choreTypeId == "Flee" || choreTypeId == "MoveToSafety" || 
                   choreTypeId == "RecoverBreath" || choreTypeId == "Die";
        }
    }
}
