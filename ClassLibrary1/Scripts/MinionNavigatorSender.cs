using ONI_MP.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Scripts
{
	internal class MinionNavigatorSender : KMonoBehaviour
	{
		[MyCmpGet] KPrefabID kpref;
		[MyCmpGet] Navigator navigator;
		public override void OnSpawn()
		{
			base.OnSpawn();

			//Subscribe((int)GameHashes.ObjectMovementStateChanged,);
			Subscribe((int)GameHashes.NavigationCellChanged, OnPathChanged);
			Subscribe((int)GameHashes.PathAdvanced, OnPathAdvanced);

		}
		void OnPathChanged(object _)
		{
			if(MultiplayerSession.InSession && MultiplayerSession.IsHost)
			{

			}
		}
		void OnPathAdvanced(object _) {
		}

	}
}
