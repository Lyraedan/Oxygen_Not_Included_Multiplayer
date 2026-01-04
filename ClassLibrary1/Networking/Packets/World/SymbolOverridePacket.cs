using ONI_MP.DebugTools;
using ONI_MP.Networking.Packets.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP.Networking.Packets.World
{
	internal class SymbolOverridePacket : IPacket
	{
		public enum Mode
		{
			AddSymbolOverride,
			RemoveSymbolOverride,
			RemoveAllSymbolsOverrides,
		}

		public int NetId;
		public Mode PacketMode;
		public HashedString Target_Symbol;
		public string Override_Symbol_Kanim;
		public KAnimHashedString Override_Symbol_Name;
		public int Priority;

		public SymbolOverridePacket() { }
		public SymbolOverridePacket(SymbolOverrideController soc, Mode mode, HashedString? target_symbol = null, KAnim.Build.Symbol? source_symbol = null, int priority = 0)
		{
			NetId = soc.GetNetId();
			PacketMode = mode;
			if (target_symbol != null)
				Target_Symbol = target_symbol.Value;
			if (source_symbol != null)
			{
				Override_Symbol_Kanim = source_symbol.build.name;
				Override_Symbol_Name = source_symbol.hash;
			}
			Priority = priority;
			//Debug.Log($"Logging SOC for {soc.name}: targetSymbol: {target_symbol}, overrideSymbolanim: {Override_Symbol_Kanim}, overrideSymbol: {Override_Symbol_Name}");
		}


		public void Serialize(BinaryWriter writer)
		{
			writer.Write(NetId);
			writer.Write((int)PacketMode);
			if (PacketMode == Mode.AddSymbolOverride || PacketMode == Mode.RemoveSymbolOverride)
				writer.Write(Target_Symbol.hash);
			if (PacketMode == Mode.AddSymbolOverride)
			{
				writer.Write(Override_Symbol_Kanim);
				writer.Write(Override_Symbol_Name.hash);
			}
			writer.Write(Priority);
		}
		public void Deserialize(BinaryReader reader)
		{
			NetId = reader.ReadInt32();
			PacketMode = (Mode)reader.ReadInt32();
			if (PacketMode == Mode.AddSymbolOverride || PacketMode == Mode.RemoveSymbolOverride)
				Target_Symbol = new HashedString(reader.ReadInt32());
			if (PacketMode == Mode.AddSymbolOverride)
			{
				Override_Symbol_Kanim = reader.ReadString();
				Override_Symbol_Name = new KAnimHashedString(reader.ReadInt32());
			}
			Priority = reader.ReadInt32();
		}


		public void OnDispatched()
		{
			if (!NetworkIdentityRegistry.TryGetComponent<SymbolOverrideController>(NetId, out var soc))
			{
				DebugConsole.LogWarning("Could not find symbolOverrideController for minion with netid " + NetId);
				return;
			}
			KAnim.Build.Symbol override_symbol = null;
			if (PacketMode == Mode.AddSymbolOverride)
			{
				if (!Override_Symbol_Kanim.Contains("_kanim"))
					Override_Symbol_Kanim += "_kanim";
				if (!Assets.TryGetAnim(Override_Symbol_Kanim, out var symbolKanim))
				{
					DebugConsole.LogWarning("Could not find kanim: " + Override_Symbol_Kanim);
					return;
				}
				var kanimData = symbolKanim.GetData();
				override_symbol = kanimData.build.GetSymbol(Override_Symbol_Name);
				if (override_symbol == null)
				{
					DebugConsole.LogWarning($"Could not find symbol {Override_Symbol_Name} in kanim {Override_Symbol_Kanim}");
					return;
				}
			}
			
			switch (PacketMode)
			{
				case Mode.AddSymbolOverride:
					soc.AddSymbolOverride(Target_Symbol, override_symbol, Priority);
					break;
				case Mode.RemoveSymbolOverride:
					soc.RemoveSymbolOverride(Target_Symbol, Priority);
					break;
				case Mode.RemoveAllSymbolsOverrides:
					soc.RemoveAllSymbolOverrides(Priority);
					break;
			}

		}
	}
}
