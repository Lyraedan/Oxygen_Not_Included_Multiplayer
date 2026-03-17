using System.Reflection;
using ONI_MP.Profiling;

namespace ONI_MP.Patches.LoadingOverlayPatch
{
	public static class LoadingOverlayExtensions
	{
		public static LoadingOverlay GetSingleton()
		{
			Profiler.Active.Scope();

			var type = typeof(LoadingOverlay);
			var field = type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
			return (LoadingOverlay)field.GetValue(null);
		}
	}
}
