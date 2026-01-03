using ONI_MP.DebugTools;
using ONI_MP.Networking;
using ONI_MP.Networking.Components;
using Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Scripts
{
	internal class MinionMultiplayerInitializer : KMonoBehaviour
	{
		[MyCmpGet] NetworkIdentity identity;
		[MyCmpGet] KPrefabID kpref;

		public override void OnSpawn()
		{
			base.OnSpawn();

			if (MultiplayerSession.InSession)
				InitializeMP(null); 

			Game.Instance?.Subscribe(MP_HASHES.OnMultiplayerGameSessionInitialized, InitializeMP);
			Game.Instance?.Subscribe(MP_HASHES.GameClient_OnConnectedInGame, InitializeMP);
		}

		void InitializeMP(object _ = null)
		{
			this.StartCoroutine(DelayedInit());
		}

		IEnumerator DelayedInit()
		{
			yield return null;
			FinalizeInit();
		}

		void FinalizeInit()
		{
			var go = gameObject;
			if (MultiplayerSession.NotInSession) return;
			if (!kpref?.HasTag(GameTags.BaseMinion) ?? false) return;

			DebugConsole.Log("OnMultiplayerGameSessionInitialized");
			// If we are a client, disable the brain/chores so the dupe is just a puppet
			if (MultiplayerSession.IsClient)
			{
				// Disable AI/decision making components
				if (go.TryGetComponent<ChoreDriver>(out var driver)) driver.enabled = false;
				if (go.TryGetComponent<ChoreConsumer>(out var consumer)) consumer.enabled = false;
				if (go.TryGetComponent<MinionBrain>(out var brain)) brain.enabled = false;
				if (go.TryGetComponent<Navigator>(out var nav)) nav.enabled = false;

				// Disable sensors that might trigger behaviors
				if (go.TryGetComponent<Sensors>(out var sensors)) sensors.enabled = false;

				// Disable state machine controllers that could override animations
				var stateMachineControllers = go.GetComponents<StateMachineController>();
				foreach (var smc in stateMachineControllers)
				{
					if (smc != null) smc.enabled = false;
				}

				// Add our client controller for receiving position/animation updates
				go.AddOrGet<DuplicantClientController>();
				DebugConsole.Log($"[DuplicantSpawn] Client setup complete for {go.name} (NetId: {identity.NetId})");
			}
			else if (MultiplayerSession.IsHost)
			{
				// Add state sender for host to broadcast duplicant state to clients
				go.AddOrGet<DuplicantStateSender>();
				DebugConsole.Log($"[DuplicantSpawn] Host setup complete for {go.name} (NetId: {identity.NetId})");
			}
		}
	}
}
