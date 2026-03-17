using HarmonyLib;
using ONI_MP.Networking.Packets.Architecture;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONI_MP.Profiling;

namespace ONI_MP.Networking.Packets.Core
{
	/// <summary>
	/// generic packet wrapper for mod API packets;
	/// each mod api registered packet type T will have its own ModApiPacket<T> type created at runtime
	/// </summary>
	/// <typeparam name="T">type of the api-registered mod class that inherits the shared IPacket</typeparam>
	internal class ModApiPacket<T> : IPacket, IModApiPacket, IPacketSkipsRegistration
	{
		public T WrappedInstance { get; private set; }
		Traverse Traverse;

		public ModApiPacket()
		{
			Profiler.Active.Scope();

			WrappedInstance = Activator.CreateInstance<T>();
			Traverse = Traverse.Create(WrappedInstance);
		}
		public void SetWrappedInstance(object instance)
		{
			Profiler.Active.Scope();

			WrappedInstance = (T)instance;
			Traverse = Traverse.Create(WrappedInstance);
		}

		public void Deserialize(BinaryReader reader)
		{
			Profiler.Active.Scope();

			Traverse.Method("Deserialize", reader).GetValue();
		}

		public void OnDispatched()
		{
			Profiler.Active.Scope();

			Traverse.Method("OnDispatched").GetValue();
		}

		public void Serialize(BinaryWriter writer)
		{
			Profiler.Active.Scope();

			Traverse.Method("Serialize", writer).GetValue();
		}
	}
}
