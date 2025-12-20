using ONI_MP.Api.Networking.Packets.Architecture;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Components;
using System.IO;

namespace ONI_MP.Networking.Packets.DuplicantActions
{
	/// <summary>
	/// Synchronizes high-level duplicant state (action type, work target, etc.)
	/// This helps clients understand what the duplicant is doing beyond just animations.
	/// </summary>
	public class DuplicantStatePacket : IPacket
	{
		public int NetId;
		public DuplicantActionState ActionState;
		public int TargetCell;          // Cell of work target (-1 if none)
		public string CurrentAnimName;  // specific animation override
		public float AnimElapsedTime;   // Elapsed time in current animation
		public bool IsWorking;          // Whether actively working on something
		public string HeldItemSymbol; // For syncing guns/tools/carryables current animation

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write((int)ActionState);
			writer.Write(TargetCell);
			writer.Write(CurrentAnimName ?? string.Empty);
			writer.Write(AnimElapsedTime);
			writer.Write(IsWorking);
			writer.Write(HeldItemSymbol ?? string.Empty);
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			ActionState = (DuplicantActionState)reader.ReadInt32(); // Changed to Int32 to match Serialize
			TargetCell = reader.ReadInt32();
			CurrentAnimName = reader.ReadString();
			AnimElapsedTime = reader.ReadSingle();
			IsWorking = reader.ReadBoolean();
			HeldItemSymbol = reader.ReadString();
		}

		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGet(NetId, out var entity))
			{
				DebugConsole.LogWarning($"[DuplicantStatePacket] NetId {NetId} not found");
				return;
			}

			var clientController = entity.GetComponent<DuplicantClientController>();
			if (clientController != null)
			{
				clientController.OnStateReceived(ActionState, TargetCell, CurrentAnimName, AnimElapsedTime, IsWorking, HeldItemSymbol);
			}
		}
	}

	/// <summary>
	/// High-level action states for duplicants
	/// </summary>
	public enum DuplicantActionState : byte
	{
		Idle = 0,
		Walking = 1,
		Working = 2,
		Building = 3,
		Digging = 4,
		Eating = 5,
		Sleeping = 6,
		Using = 7,       // Using a machine/station
		Carrying = 9,
		Climbing = 10,
		Swimming = 11,
		Falling = 12,
		Disinfecting = 13,
		Other = 100
	}
}
