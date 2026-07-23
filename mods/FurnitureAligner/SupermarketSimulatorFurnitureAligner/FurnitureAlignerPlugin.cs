using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace SupermarketSimulatorFurnitureAligner;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("SirW_CustomHints", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class FurnitureAlignerPlugin : BasePlugin
{
	public const string PluginGuid = "CS.supermarketsimulator.furniturealigner";

	public const string PluginName = "CS - Furniture Aligner";

	public const string PluginVersion = "1.2.5";

	internal static ManualLogSource LogSource;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<bool> EdgeAlignEnabled;

	internal static ConfigEntry<bool> CenterLineEnabled;

	internal static ConfigEntry<bool> GridSnapEnabled;

	internal static ConfigEntry<bool> AllowOutside;

	internal static ConfigEntry<bool> ShowPlacementHints;

	internal static ConfigEntry<KeyCode> ToggleKey;

	internal static ConfigEntry<KeyCode> EdgeAlignKey;

	internal static ConfigEntry<KeyCode> CenterLineKey;

	internal static ConfigEntry<KeyCode> GridSnapKey;

	internal static ConfigEntry<KeyCode> OutsideKey;

	internal static ConfigEntry<KeyCode> NudgeUpKey;

	internal static ConfigEntry<KeyCode> NudgeDownKey;

	internal static ConfigEntry<KeyCode> NudgeLeftKey;

	internal static ConfigEntry<KeyCode> NudgeRightKey;

	internal static ConfigEntry<KeyCode> NudgeResetKey;

	internal static ConfigEntry<float> SnapDistance;

	internal static ConfigEntry<float> EdgeSnapDistance;

	internal static ConfigEntry<float> SnapSearchRadius;

	internal static ConfigEntry<float> SnapGap;

	internal static ConfigEntry<float> NudgeStep;

	internal static ConfigEntry<float> GridSize;

	internal static ConfigEntry<float> GridOriginX;

	internal static ConfigEntry<float> GridOriginZ;

	private Harmony _harmony;

	public override void Load()
	{
		LogSource = Log;
		DeleteOldConfigFiles("sx930.supermarketsimulator.furniturealigner");
		Enabled = Config.Bind("General", "Enabled", false, "Master switch for the mod.");
		EdgeAlignEnabled = Config.Bind("General", "EdgeAlignEnabled", false, "Align furniture edges with nearby furniture-like placeable objects.");
		CenterLineEnabled = Config.Bind("General", "CenterLineEnabled", false, "Align center lines with nearby furniture-like placeable objects only.");
		GridSnapEnabled = Config.Bind("General", "GridSnapEnabled", false, "Snap held furniture to a virtual world-space grid.");
		AllowOutside = Config.Bind("General", "AllowOutside", false, "Bypass placement checks. This can allow outside placement, but can also allow clipping.");
		ShowPlacementHints = Config.Bind("General", "ShowPlacementHints", true, "Show Furniture Aligner keybind hints in the left interaction hint panel while placing (requires SirW_CustomHints).");
		ToggleKey = Config.Bind("Hotkeys", "ToggleKey", KeyCode.F8, "Toggle the mod on/off.");
		EdgeAlignKey = Config.Bind("Hotkeys", "EdgeAlignKey", KeyCode.U, "Toggle edge alignment.");
		CenterLineKey = Config.Bind("Hotkeys", "CenterLineKey", KeyCode.I, "Toggle center-line alignment.");
		GridSnapKey = Config.Bind("Hotkeys", "GridSnapKey", KeyCode.O, "Toggle virtual grid snapping.");
		OutsideKey = Config.Bind("Hotkeys", "OutsideKey", KeyCode.F9, "Toggle bypassing placement checks.");
		NudgeUpKey = Config.Bind("Hotkeys", "NudgeUpKey", KeyCode.UpArrow, "Move the held furniture forward by one small step.");
		NudgeDownKey = Config.Bind("Hotkeys", "NudgeDownKey", KeyCode.DownArrow, "Move the held furniture backward by one small step.");
		NudgeLeftKey = Config.Bind("Hotkeys", "NudgeLeftKey", KeyCode.LeftArrow, "Move the held furniture left by one small step.");
		NudgeRightKey = Config.Bind("Hotkeys", "NudgeRightKey", KeyCode.RightArrow, "Move the held furniture right by one small step.");
		NudgeResetKey = Config.Bind("Hotkeys", "NudgeResetKey", KeyCode.Keypad5, "Reset manual nudge offset for the currently held furniture.");
		SnapDistance = Config.Bind("Tuning", "SnapDistance", 60f, "Center-line snap distance.");
		EdgeSnapDistance = Config.Bind("Tuning", "EdgeSnapDistance", 15f, "Edge snap distance.");
		SnapSearchRadius = Config.Bind("Tuning", "SnapSearchRadius", 100f, "Nearby furniture search range.");
		SnapGap = Config.Bind("Tuning", "SnapGap", 3f, "Gap between edge-snapped furniture.");
		NudgeStep = Config.Bind("Tuning", "NudgeStep", 3f, "Distance moved by each nudge key press.");
		GridSize = Config.Bind("Tuning", "GridSize", 10f, "Virtual grid cell size.");
		GridOriginX = Config.Bind("Tuning", "GridOriginX", 0f, "Virtual grid origin on the world X axis.");
		GridOriginZ = Config.Bind("Tuning", "GridOriginZ", 0f, "Virtual grid origin on the world Z axis.");
		EnforceSingleSnapMode();
		FurnitureAlignerHints.Initialize();
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(FurnitureAlignerPlugin).Assembly);
		Log.LogInfo("CS - Furniture Aligner loaded. Placement hints: " + (FurnitureAlignerHints.IsAvailable ? "CustomHints ready" : "CustomHints not found (optional)"));
	}

	private static void DeleteOldConfigFiles(params string[] oldGuids)
	{
		foreach (string text in oldGuids)
		{
			string path = Path.Combine(Paths.ConfigPath, text + ".cfg");
			if (!File.Exists(path))
			{
				continue;
			}
			try
			{
				File.Delete(path);
				LogSource.LogInfo("Deleted old config file: " + Path.GetFileName(path));
			}
			catch (Exception ex)
			{
				LogSource.LogWarning("Could not delete old config file " + Path.GetFileName(path) + ": " + ex.Message);
			}
		}
	}

	private static void EnforceSingleSnapMode()
	{
		if (EdgeAlignEnabled.Value)
		{
			CenterLineEnabled.Value = false;
			GridSnapEnabled.Value = false;
		}
		else if (CenterLineEnabled.Value)
		{
			GridSnapEnabled.Value = false;
		}
	}
}
