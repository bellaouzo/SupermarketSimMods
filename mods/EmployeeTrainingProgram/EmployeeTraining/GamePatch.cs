using HarmonyLib;

namespace EmployeeTraining;

[HarmonyPatch]
public static class GamePatch
{
	public static Plugin plugin;

	[HarmonyPatch(typeof(MainMenuManager), "NewGame")]
	[HarmonyPrefix]
	public static bool MainMenuManager_NewGame_Prefix()
	{
		ETSaveManager.IsReadyToSave = true;
		return true;
	}

	[HarmonyPatch(typeof(MainMenuManager), "NewGame")]
	[HarmonyPostfix]
	public static void MainMenuManager_NewGame_Postfix()
	{
		ETSaveManager.Clear();
	}

	[HarmonyPatch(typeof(DailyStatisticsScreen), "StartNewGame")]
	[HarmonyPrefix]
	public static bool BankruptcyManager_CheckForBankruptcy_Postfix()
	{
		ETSaveManager.Clear();
		Plugin.Instance.GameQuitEvent?.Invoke();
		return true;
	}

	[HarmonyPatch(typeof(SaveManager), "Awake")]
	[HarmonyPostfix]
	public static void SaveManager_Awake_Postfix(SaveManager __instance)
	{
		ETSaveManager.Load(__instance.CurrentSaveFilePath);
	}

	[HarmonyPatch(typeof(VehicleDataLoader), "ApplyVehicleData")]
	[HarmonyPrefix]
	public static bool VehicleDataLoader_ApplyVehicleData_Prefix(VehicleDataLoader __instance)
	{
		__instance.VehicleData.Boxes.RemoveAll((Il2CppSystem.Predicate<BoxData>)((BoxData box) => box.ProductID == -1));
		return true;
	}
}
