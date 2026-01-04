using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Networking.Packets.Animation
{
	internal class SymbolVisibilityTogglePacket : IPacket
	{
		int NetId;
		KAnimHashedString Symbol;
		bool Is_Visible;

		public SymbolVisibilityTogglePacket() { }
		public SymbolVisibilityTogglePacket(KBatchedAnimController kbac, KAnimHashedString symbol, bool is_visible)
		{
			NetId = kbac.GetNetId();
			Symbol = symbol;
			Is_Visible = is_visible;
		}

		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			Symbol = new KAnimHashedString(reader.ReadInt32());
			Is_Visible = reader.ReadBoolean();

		}


		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write(Symbol.hash);
			writer.Write(Is_Visible);
		}
		public void OnDispatched()
		{
			if (MultiplayerSession.IsHost)
				return;

			if (!NetworkIdentityRegistry.TryGetComponent<KBatchedAnimController>(NetId,out var kbac))
				return;
			kbac.SetSymbolVisiblity(Symbol, Is_Visible);
		}
	}
}
