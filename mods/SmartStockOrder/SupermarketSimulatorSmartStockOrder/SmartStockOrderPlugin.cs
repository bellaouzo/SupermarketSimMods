using System;
using Object = UnityEngine.Object;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("SirW_CustomHints", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class SmartStockOrderPlugin : BasePlugin
{
	public const string PluginGuid = "CS.supermarketsimulator.smartstockorder";

	public const string PluginName = "CS - Smart Stock Order";

	public const string PluginVersion = "1.2.13";

	internal static ManualLogSource LogSource;

	internal static ConfigEntry<bool> Enabled;

	internal static ConfigEntry<bool> IncludeDisplayShelfShortage;

	internal static ConfigEntry<bool> IncludeRackShortage;

	internal static ConfigEntry<bool> IncludeCrateShortage;

	internal static ConfigEntry<bool> MinimumUsesBoxStockOnly;

	internal static ConfigEntry<bool> RemoveCartLimit;

	internal static ConfigEntry<int> CartLimitOverride;

	internal static ConfigEntry<int> MaxBoxesPerRun;

	internal static ConfigEntry<float> MarketButtonScale;

	internal static ConfigEntry<bool> ShowHints;

	internal static ConfigEntry<KeyCode> TabletRefillKey;

	internal static ConfigEntry<KeyCode> TabletMinimumKey;

	private Harmony _harmony;

	public override void Load()
	{
		LogSource = Log;
		DeleteOldConfigFiles("sx930.supermarketsimulator.smartstockorder");
		Enabled = Config.Bind("General", "Enabled", true, "Enable auto stock ordering.");
		ShowHints = Config.Bind("General", "ShowHints", true, "Show V/B stock-order hints in the left interaction hint panel while the tablet/scanner is open (requires SirW_CustomHints).");
		IncludeDisplayShelfShortage = Config.Bind("Ordering", "IncludeDisplayShelfShortage", false, "Legacy option. Refill All now orders by labeled box rack shortage only.");
		IncludeRackShortage = Config.Bind("Ordering", "IncludeRackShortage", true, "Full-order mode counts empty space on labeled box rack slots.");
		IncludeCrateShortage = Config.Bind("Ordering", "IncludeCrateShortage", false, "Legacy option. Fruit/vegetable ordering is based on labeled box rack slots as 4 boxes per slot.");
		MinimumUsesBoxStockOnly = Config.Bind("Ordering", "MinimumUsesBoxStockOnly", true, "Minimum mode orders one box when the product has no boxes on racks. When false, empty display shelves also trigger one box.");
		RemoveCartLimit = Config.Bind("Cart", "RemoveCartLimit", true, "Remove the market cart item-count limit while this mod is enabled.");
		CartLimitOverride = Config.Bind("Cart", "CartLimitOverride", 9999, "Internal cart item limit used when RemoveCartLimit is enabled.");
		MaxBoxesPerRun = Config.Bind("Safety", "MaxBoxesPerRun", 500, "Maximum boxes this mod may add to the cart in one click or hotkey press.");
		MarketButtonScale = Config.Bind("UI", "MarketButtonScale", 0.55f, "Visual scale for the Fill Racks / 1 Box Empty Market App buttons (taskbar-sized).");
		TabletRefillKey = Config.Bind("Hotkeys", "TabletRefillKey", KeyCode.V, "Run Fill Empty Racks from anywhere.");
		TabletMinimumKey = Config.Bind("Hotkeys", "TabletMinimumKey", KeyCode.B, "Run 1 Box If Empty from anywhere.");
		MigrateOldTabletHotkeys();
		MigrateOldButtonScale();
		SmartStockOrderHints.Initialize();
		ClassInjector.RegisterTypeInIl2Cpp<SmartStockOrderMarketButtons>();
		ClassInjector.RegisterTypeInIl2Cpp<SmartStockOrderTabletShortcuts>();
		GameObject val = new GameObject("SmartStockOrder_MarketButtons");
		Object.DontDestroyOnLoad(val);
		val.hideFlags = HideFlags.HideAndDontSave;
		val.AddComponent<SmartStockOrderMarketButtons>();
		val.AddComponent<SmartStockOrderTabletShortcuts>();
		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(SmartStockOrderPlugin).Assembly);
		Log.LogInfo("CS - Smart Stock Order loaded. Market buttons or V/B hotkeys. Hints: " + (SmartStockOrderHints.IsAvailable ? "CustomHints ready" : "CustomHints not found (optional)"));
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

	private void MigrateOldTabletHotkeys()
	{
		bool flag = false;
		if (TabletRefillKey.Value == KeyCode.LeftBracket)
		{
			TabletRefillKey.Value = KeyCode.V;
			flag = true;
		}
		if (TabletMinimumKey.Value == KeyCode.RightBracket)
		{
			TabletMinimumKey.Value = KeyCode.B;
			flag = true;
		}
		if (flag)
		{
			Config.Save();
			Log.LogInfo("Smart Stock Order: migrated old tablet hotkeys to V/B.");
		}
	}

	private void MigrateOldButtonScale()
	{
		float value = MarketButtonScale.Value;
		if (Mathf.Abs(value - 0.42f) > 0.001f && Mathf.Abs(value - 0.9f) > 0.001f)
		{
			return;
		}
		MarketButtonScale.Value = 0.55f;
		Config.Save();
		Log.LogInfo("Smart Stock Order: migrated MarketButtonScale " + value.ToString("0.##") + " -> 0.55 for compact taskbar buttons.");
	}
}
