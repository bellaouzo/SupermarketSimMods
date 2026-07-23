using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace DemandSystem;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DemandPlugin : BasePlugin
{
	public const string PluginGuid = "DemandSystem";
	public const string PluginName = "Demand System";
	public const string PluginVersion = "1.1.1";

	internal static ConfigEntry<bool> Enabled;
	internal static ConfigEntry<float> EventChancePercent;
	internal static ConfigEntry<float> TwoProductsChancePercent;
	internal static ConfigEntry<float> ThreeProductsChancePercent;
	internal static ConfigEntry<int> ExtraItemsMin;
	internal static ConfigEntry<int> ExtraItemsMax;
	internal static ConfigEntry<int> CustomerDemandChancePercent;
	internal static ConfigEntry<float> OverlayDurationSeconds;
	internal static ConfigEntry<bool> KeepOverlayVisibleForDebug;
	internal static ConfigEntry<bool> DebugLogging;

	internal static ManualLogSource LogSource;

	private Harmony _harmony;

	public override void Load()
	{
		LogSource = base.Log;
		Enabled = Config.Bind("Demand", "Enabled", true, "Turns the demand system on or off.");
		EventChancePercent = Config.Bind("Demand", "EventChancePercent", 30f, "Chance for a high-demand event each day.");
		TwoProductsChancePercent = Config.Bind("Demand", "TwoProductsChancePercent", 15f, "Chance an event uses exactly 2 products.");
		ThreeProductsChancePercent = Config.Bind("Demand", "ThreeProductsChancePercent", 5f, "Chance an event uses exactly 3 products.");
		CustomerDemandChancePercent = Config.Bind("Demand", "CustomerDemandChancePercent", 30, "Chance each customer list gets demanded products.");
		ExtraItemsMin = Config.Bind("Demand", "ExtraItemsMin", 1, "Minimum extra units per demanded product.");
		ExtraItemsMax = Config.Bind("Demand", "ExtraItemsMax", 2, "Maximum extra units per demanded product.");
		OverlayDurationSeconds = Config.Bind("UI", "OverlayDurationSeconds", 12f, "Seconds the overlay stays visible after generation.");
		KeepOverlayVisibleForDebug = Config.Bind("UI", "KeepOverlayVisibleForDebug", false, "Keep overlay visible all day when a demand event is active.");
		DebugLogging = Config.Bind("Diagnostics", "DebugLogging", false, "Verbose demand logging.");

		ClassInjector.RegisterTypeInIl2Cpp<DemandOverlay>();
		DemandOverlay.Create();
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(DemandPlugin).Assembly);
		base.Log.LogInfo($"{PluginName} {PluginVersion} loaded (co-op synced day seed).");
	}
}
