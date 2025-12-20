using ONI_MP.Api.Networking;
using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.DuplicantActions;
using UnityEngine;

namespace ONI_MP.Networking.Components
{
	/// <summary>
	/// Host-side component that monitors duplicant state and sends updates to clients.
	/// Tracks chores, animations, and work status.
	/// </summary>
	public class DuplicantStateSender : KMonoBehaviour
	{
		private NetworkIdentity networkIdentity;
		private KAnimControllerBase animController;
		private ChoreDriver choreDriver;
		private Navigator navigator;

		private float sendInterval = 0.2f; // 200ms state updates (less frequent than position)
		private float timer;

		private DuplicantActionState lastSentState;
		private int lastSentTargetCell;
		private string lastSentAnimName;
		private bool lastSentIsWorking;

		public override void OnSpawn()
		{
			base.OnSpawn();

			networkIdentity = GetComponent<NetworkIdentity>();
			animController = GetComponent<KAnimControllerBase>();
			choreDriver = GetComponent<ChoreDriver>();
			navigator = GetComponent<Navigator>();

			if (networkIdentity == null)
			{
				DebugConsole.LogWarning($"[DuplicantStateSender] {gameObject.name} missing NetworkIdentity");
				enabled = false;
				return;
			}

			// Only active on host
			if (MultiplayerSession.IsClient)
			{
				enabled = false;
				return;
			}
		}

		private void Update()
		{
			if (!MultiplayerSession.InSession || MultiplayerSession.IsClient)
				return;

			timer += Time.unscaledDeltaTime;
			heartbeatTimer += Time.unscaledDeltaTime;

			if (timer < sendInterval)
				return;

			timer = 0f;
			SendStatePacket();
		}

		private float heartbeatTimer;
		private float heartbeatInterval = 1.0f; // Force send every 1s

		private void SendStatePacket()
		{
			try
			{
				var state = DetermineCurrentState();
				int targetCell = DetermineTargetCell();
				string animName = GetCurrentAnimName();
				bool isWorking = IsCurrentlyWorking();
				string heldSymbol = DetermineHeldItemSymbol();
				float animElapsedTime = animController != null ? animController.GetElapsedTime() : 0f;

				// Only send if something changed (or periodically for sync)
				bool stateChanged = state != lastSentState ||
														targetCell != lastSentTargetCell ||
														animName != lastSentAnimName ||
														isWorking != lastSentIsWorking ||
														heldSymbol != lastSentHeldSymbol;

				// Heartbeat: Force send if enough time passed, even if no change
				bool isHeartbeat = heartbeatTimer >= heartbeatInterval;

				if (!stateChanged && !isHeartbeat)
					return;

				if (isHeartbeat) heartbeatTimer = 0f;

				lastSentState = state;
				lastSentTargetCell = targetCell;
				lastSentAnimName = animName;
				lastSentIsWorking = isWorking;
				lastSentHeldSymbol = heldSymbol;

				var packet = new DuplicantStatePacket
				{
					NetId = networkIdentity.NetId,
					ActionState = state,
					TargetCell = targetCell,
					CurrentAnimName = animName,
					AnimElapsedTime = animElapsedTime,
					IsWorking = isWorking,
					HeldItemSymbol = heldSymbol
				};

				PacketSender.SendToAllClients(packet, sendType: SteamNetworkingSend.Unreliable);
			}
			catch (System.Exception)
			{
				// Silently ignore - state may not be ready yet
			}
		}

		private string lastSentHeldSymbol;

		private string DetermineHeldItemSymbol()
		{
			// Check for SymbolOverrideController
			var symbolOverride = GetComponent<SymbolOverrideController>();
			if (symbolOverride == null) return string.Empty;

			// SnapTo is the typical "hand" override for tools/carryables
			// Common target symbols: "snapto_pivot", "snapto_r_arm"
			// We need to find if any symbol is overriding these

			// Using reflection to inspect private dictionary if needed, but GetSymbolOverride might work if we know the target
			// Actually, we want to know WHICH symbol is applied to the 'snapto_pivot'

			// ONI API: symbolOverride.GetSymbolOverride(HashedString target_symbol)
			var target = new HashedString("snapto_pivot");
			var overrideIdx = symbolOverride.GetSymbolOverrideIdx(target);

			if (overrideIdx != -1)
			{
				// We have an override. We need the name of the symbol that is overriding.
				// KAnimBatch provides access? This is tricky.
				// Simpler approach: Check commonly used symbols for tools
				// or check what chore driver is using.

				// Alternative: just send the symbol ID if we can resolve it on client?
				// No, we need the BatchTag/SymbolName.

				// Let's try retrieving the active batch/symbol from the controller
				// KAnim.Build.Symbol symbol = symbolOverride.GetSymbol(overrideIdx) ??
			}

			// Fallback: Check FetchChore / Workable to guess what we are holding
			if (choreDriver != null)
			{
				var chore = choreDriver.GetCurrentChore();
				if (chore != null)
				{
					if (chore.target != null)
					{
						var fetchChore = chore as FetchChore;
						if (fetchChore != null && fetchChore.target != null)
						{
							// return fetchChore.target.name; // This is the object name, might match symbol?
						}
					}
				}
			}

			// For building gun specifically:
			// It uses a specific symbol override "gun" or "build_tool" often.
			// If we are building, we usually have a gun.
			var state = DetermineCurrentState();
			if (state == DuplicantActionState.Building || state == DuplicantActionState.Digging || state == DuplicantActionState.Disinfecting)
			{
				return "gun"; // This effectively tells client to equip gun
			}

			return string.Empty;
		}

		private DuplicantActionState DetermineCurrentState()
		{
			if (choreDriver == null || choreDriver.GetCurrentChore() == null)
			{
				// Check if moving
				if (navigator != null && navigator.IsMoving())
				{
					return GetNavTypeState(navigator.CurrentNavType);
				}
				return DuplicantActionState.Idle;
			}

			var chore = choreDriver.GetCurrentChore();
			var choreType = chore.choreType;

			if (choreType == null)
				return DuplicantActionState.Other;

			// Map chore type to action state
			string choreId = choreType.Id;

			if (choreId.Contains("Build") || choreId.Contains("Construct"))
				return DuplicantActionState.Building;
			if (choreId.Contains("Dig") || choreId.Contains("Uproot"))
				return DuplicantActionState.Digging;
			if (choreId.Contains("Eat") || choreId.Contains("Food"))
				return DuplicantActionState.Eating;
			if (choreId.Contains("Sleep"))
				return DuplicantActionState.Sleeping;
			if (choreId.Contains("Fetch") || choreId.Contains("Deliver") || choreId.Contains("Storage"))
				return DuplicantActionState.Carrying;

			// Check if moving to work
			if (navigator != null && navigator.IsMoving())
			{
				return GetNavTypeState(navigator.CurrentNavType);
			}

			// Default to working for any other chore
			return DuplicantActionState.Working;
		}

		private DuplicantActionState GetNavTypeState(NavType navType)
		{
			if (navType == NavType.Ladder || navType == NavType.Pole)
				return DuplicantActionState.Climbing;
			if (navType == NavType.Swim)
				return DuplicantActionState.Swimming;
			return DuplicantActionState.Walking;
		}

		private int DetermineTargetCell()
		{
			if (choreDriver == null)
				return -1;

			var chore = choreDriver.GetCurrentChore();
			if (chore != null && chore.gameObject != null)
			{
				return Grid.PosToCell(chore.gameObject);
			}
			return -1;
		}

		private string GetCurrentAnimName()
		{
			if (animController == null)
				return string.Empty;

			return animController.currentAnim.ToString();
		}

		private bool IsCurrentlyWorking()
		{
			if (choreDriver == null)
				return false;

			var chore = choreDriver.GetCurrentChore();
			if (chore == null)
				return false;

			// Check if this is a work-type chore (not movement)
			if (navigator != null && navigator.IsMoving())
				return false;

			return true;
		}
	}
}
