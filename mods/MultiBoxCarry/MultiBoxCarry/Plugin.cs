using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace MultiBoxCarry;

[BepInPlugin("com.yaboie88.multiboxcarry", "Multi Box Carry", PluginVersion)]
public class Plugin : BasePlugin
{
	internal const string PluginVersion = "1.1.33";

	internal new static ManualLogSource Log;

	private Harmony _harmony;

	private static bool _initialized;

	public override void Load()
	{
		if (_initialized)
		{
			((BasePlugin)this).Log.LogInfo((object)"MultiBoxCarry already initalized. Skipping Duplicate.");
			return;
		}

		_initialized = true;
		Log = ((BasePlugin)this).Log;
		PluginConfig.Bind(Config);
		Log.LogInfo((object)"Multi Box Carry loading...");
		_harmony = new Harmony("com.yaboie88.multiboxcarry");
		_harmony.PatchAll();
		ClassInjector.RegisterTypeInIl2Cpp<BoxInventoryHUD>();
		GameObject hudObject = new GameObject("MultiBoxCarry_HUD");
		Object.DontDestroyOnLoad((Object)(object)hudObject);
		hudObject.AddComponent<BoxInventoryHUD>();
		Log.LogInfo((object)("Multi Box Carry " + PluginVersion + " loaded."));
	}
}
