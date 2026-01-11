using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ONI_MP.Patches.Duplicant
{
	internal class Flatulence_Patch
	{

		[HarmonyPatch(typeof(Flatulence), nameof(Flatulence.Emit))]
		public class Flatulence_Emit_Patch
		{
			/// <summary>
			/// Skip farting for printing pod preview duplicants
			/// </summary>
			/// <param name="data"></param>
			/// <returns></returns>
			public static bool Prefix(object data)
			{
				GameObject gameObject = (GameObject)data;
				return (gameObject.PrefabID() != GameTags.MinionSelectPreview);
			}
		}
	}
}
