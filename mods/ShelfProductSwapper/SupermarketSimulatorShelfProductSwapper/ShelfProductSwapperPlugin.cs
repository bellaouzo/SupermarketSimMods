using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace SupermarketSimulatorShelfProductSwapper;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("SirW_CustomHints", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class ShelfProductSwapperPlugin : BasePlugin
{
	public const string PluginGuid = "CS.supermarketsimulator.shelfproductswapper";

	public const string PluginName = "CS - Shelf Product Swapper";

	public const string PluginVersion = "1.2.13";

	internal static ManualLogSource LogSource;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<KeyCode> SwapKey;

	internal static ConfigEntry<float> RayDistance;

	internal static ConfigEntry<float> CenterSelectionRadius;

	internal static ConfigEntry<float> RackEmptyMarkerYOffset;

	internal static ConfigEntry<bool> AllowEmptySlots;

	internal static ConfigEntry<bool> RequireCompatibleDisplayType;

	internal static ConfigEntry<bool> AllowIncompatibleDisplayTypes;

	internal static ConfigEntry<bool> ShowSelectionMarkers;

	internal static ConfigEntry<bool> UseNativeOutlines;

	internal static ConfigEntry<bool> ShowHints;

	internal static ConfigEntry<KeyCode> ToggleModKey;

	internal static ConfigEntry<KeyCode> ToggleIncompatibleKey;

	private Harmony _harmony;

	public override void Load()
	{
		LogSource = Log;
		DeleteOldConfigFiles("sx930.supermarketsimulator.shelfproductswapper");
		Enabled = Config.Bind("General", "Enabled", true, "Master switch for the shelf product swapper.");
		AllowEmptySlots = Config.Bind("General", "AllowEmptySlots", true, "Allow swapping a filled shelf slot with an empty shelf slot.");
		RequireCompatibleDisplayType = Config.Bind("General", "RequireCompatibleDisplayType", true, "Only allow products to move to a display type they are made for, such as shelf products staying on shelves.");
		AllowIncompatibleDisplayTypes = Config.Bind("General", "AllowIncompatibleDisplayTypes", false, "When enabled, allow swapping products across incompatible furniture display types.");
		ShowHints = Config.Bind("General", "ShowHints", true, "Show swapper keybind hints in the left interaction hint panel while enabled (requires SirW_CustomHints).");
		ToggleModKey = Config.Bind("Hotkeys", "ToggleModKey", KeyCode.F6, "Toggle the shelf product swapper on/off.");
		SwapKey = Config.Bind("Hotkeys", "SwapKey", KeyCode.C, "Select one shelf slot, then select another shelf slot to swap their products.");
		ToggleIncompatibleKey = Config.Bind("Hotkeys", "ToggleIncompatibleKey", KeyCode.X, "Toggle whether incompatible furniture display type swaps are allowed.");
		RayDistance = Config.Bind("Tuning", "RayDistance", 5f, "Maximum distance from the camera to a shelf slot, label, or price tag.");
		CenterSelectionRadius = Config.Bind("Tuning", "CenterSelectionRadius", 0.14f, "How close a shelf slot must be to the center of the screen when raycast selection misses.");
		RackEmptyMarkerYOffset = Config.Bind("Tuning", "RackEmptyMarkerYOffset", -0.18f, "Vertical offset for box rack empty-slot markers, relative to the detected slot/label bounds bottom.");
		ShowSelectionMarkers = Config.Bind("Visuals", "ShowSelectionMarkers", true, "Show colored markers for the aimed shelf slot and selected shelf slot.");
		UseNativeOutlines = Config.Bind("Visuals", "UseNativeOutlines", false, "Also toggle the game's highlight outline. Off by default — outlining whole shelves is expensive and causes FPS drops.");
		ShelfProductSwapperHints.Initialize();
		NetworkShelfSync.EnsureCallbacks();
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(ShelfProductSwapperPlugin).Assembly);
		Log.LogInfo("CS - Shelf Product Swapper loaded. F6=toggle, X=cross-type, C=select/swap. Hints: " + (ShelfProductSwapperHints.IsAvailable ? "CustomHints ready" : "CustomHints not found (optional)"));
	}

	private static void DeleteOldConfigFiles(params string[] oldGuids)
	{
		foreach (string text in oldGuids)
		{
			string path = Path.Combine(Paths.ConfigPath, text + ".cfg");
			if (File.Exists(path))
			{
				try
				{
					File.Delete(path);
					LogSource.LogInfo((object)("Deleted old config file: " + Path.GetFileName(path)));
				}
				catch (Exception ex)
				{
					LogSource.LogWarning((object)("Could not delete old config file " + Path.GetFileName(path) + ": " + ex.Message));
				}
			}
		}
	}
}
