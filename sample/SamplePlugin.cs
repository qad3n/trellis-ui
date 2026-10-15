using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace TrellisSample
{
	[BepInPlugin("example.trellis.sample", "Trellis Sample", "1.0.0")]
	public class SamplePlugin : BasePlugin
	{
		private static ManualLogSource m_log;

		public override void Load()
		{
			SamplePlugin.m_log = Log;
			UiLog.bind(Log.LogDebug, Log.LogInfo, Log.LogWarning, Log.LogDebug, SamplePlugin.logException, () => false);
			SampleMenu.install();

			ClassInjector.RegisterTypeInIl2Cpp<SampleDriver>();
			AddComponent<SampleDriver>();
		}

		private static void logException(string location, Exception ex) => SamplePlugin.m_log.LogError($"{location}: {ex}");
	}
}
