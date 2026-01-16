using HarmonyLib;
using Shared.Helpers;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ONI_MP_API
{
	public static class SessionInfoAPI
	{
		static bool Init()
		{
			if (typesInitialized)
				return true;

			if (!ReflectionHelper.TryGetFieldInfo("ONI_MP.Networking.MultiplayerSession, ONI_MP", "InSession", out _InSessionFieldInfo))
				return false;

			if (!ReflectionHelper.TryGetPropertyGetter("ONI_MP.Networking.MultiplayerSession, ONI_MP", "IsClient", out _IsClientGetter))
				return false;

			if (!ReflectionHelper.TryGetPropertyGetter("ONI_MP.Networking.MultiplayerSession, ONI_MP", "IsHost", out _IsHostGetter))
				return false;
			if (!ReflectionHelper.TryGetPropertyGetter("ONI_MP.Networking.MultiplayerSession, ONI_MP", "LocalSteamID", out _LocalSteamIDGetter))
				return false;
			if (!ReflectionHelper.TryGetPropertyGetter("ONI_MP.Networking.MultiplayerSession, ONI_MP", "HostSteamID", out _HostSteamIDGetter))
				return false;


			typesInitialized = true;
			return true;
		}

		///not sure where this belongs best
		public static bool MultiplayerModPresent => MP_Mod_Info.MultiplayerModPresent;

		static bool typesInitialized = false;

		static FieldInfo _InSessionFieldInfo;
		static MethodInfo _IsHostGetter, _IsClientGetter, _LocalSteamIDGetter, _HostSteamIDGetter;

		public static bool InSession
		{
			get
			{
				Init();
				if (_InSessionFieldInfo == null)
					return false;
				return (bool)_InSessionFieldInfo.GetValue(null);
			}
		}
		public static bool IsHost
		{
			get
			{
				Init();
				if (_IsHostGetter == null)
					return false;
				return (bool)_IsHostGetter.Invoke(null, null);
			}
		}
		public static bool IsClient
		{
			get
			{
				Init();
				if (_IsClientGetter == null)
					return false;
				return (bool)_IsClientGetter.Invoke(null, null);
			}
		}
		public static ulong LocalSteamID
		{
			get
			{
				Init();
				if (_LocalSteamIDGetter == null)
					return 0uL;
				return (ulong) _LocalSteamIDGetter.Invoke(null, null);
			}
		}
		public static ulong HostSteamID
		{
			get
			{
				Init();
				if (_HostSteamIDGetter == null)
					return 0uL;
				return (ulong)_HostSteamIDGetter.Invoke(null, null);
			}
		}

	}
}
