using System;
using HarmonyLib;

namespace EmployeeTraining;

[HarmonyPatch]
public static class SaveManager_Save_Patcher
{
	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), new Type[] { })]
	[HarmonyPostfix]
	private static void Save_Postfix(SaveManager __instance)
	{
		ETSaveManager.SaveCurrent(force: true);
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), new Type[] { typeof(SaveInfo) })]
	[HarmonyPostfix]
	private static void Save_SaveInfo_Postfix(SaveManager __instance)
	{
		ETSaveManager.SaveCurrent(force: true);
	}

	[HarmonyPatch(typeof(SaveManager), "Save", new Type[] { typeof(string) })]
	[HarmonyPostfix]
	private static void Save_String_Postfix(SaveManager __instance, string saveName)
	{
		if (!string.IsNullOrEmpty(saveName))
		{
			ETSaveManager.Save(saveName);
			return;
		}
		ETSaveManager.SaveCurrent(force: true);
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Load), new Type[] { typeof(SaveInfo) })]
	[HarmonyPostfix]
	private static void Load_SaveInfo_Postfix(SaveManager __instance)
	{
		ETSaveManager.Load(__instance.CurrentSaveFilePath);
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.LoadLast))]
	[HarmonyPostfix]
	private static void LoadLast_Postfix(SaveManager __instance)
	{
		ETSaveManager.Load(__instance.CurrentSaveFilePath);
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.ApplySaveData))]
	[HarmonyPostfix]
	private static void ApplySaveData_Postfix(SaveManager __instance)
	{
		ETSaveManager.Load(__instance.CurrentSaveFilePath);
	}

	[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.CreateLoadNewSave))]
	[HarmonyPostfix]
	private static void CreateLoadNewSave_Postfix(SaveManager __instance)
	{
		ETSaveManager.IsReadyToSave = true;
		ETSaveManager.Load(__instance.CurrentSaveFilePath);
	}
}
