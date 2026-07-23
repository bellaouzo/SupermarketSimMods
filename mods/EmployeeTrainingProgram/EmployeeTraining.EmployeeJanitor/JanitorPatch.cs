using System;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using __Project__.Scripts.Janitor;
using __Project__.Scripts.Multiplayer;

namespace EmployeeTraining.EmployeeJanitor;

[HarmonyPatch]
public static class JanitorPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "SpawnJanitor")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnJanitor_Postfix(EmployeeManager __instance, int janitorID)
	{
		List<Janitor> activeJanitor = __instance.ActiveJanitor ?? __instance.m_ActiveJanitor;
		if (activeJanitor != null)
		{
			JanitorSkillManager.Instance.Spawn(activeJanitor, janitorID);
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireJanitor")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireJanitor_Postfix(EmployeeManager __instance, int janitorID)
	{
		JanitorSkillManager.Instance.Fire(janitorID);
	}

	[HarmonyPatch(typeof(EmployeeManager), "DespawnJanitor")]
	[HarmonyPrefix]
	public static void CheckoutManager_DespawnJanitor_Prefix(EmployeeManager __instance, int janitorID)
	{
		List<Janitor> active = __instance.ActiveJanitor ?? __instance.m_ActiveJanitor;
		if (active == null)
		{
			return;
		}
		Janitor val = null;
		foreach (Janitor janitor in active)
		{
			if (janitor != null && janitor.JanitorID == janitorID)
			{
				val = janitor;
				break;
			}
		}
		if (val != null)
		{
			JanitorSkillManager.Instance.Despawn(val);
		}
	}

	[HarmonyPatch(typeof(Janitor), "SetJanitorBoost", new Type[] { typeof(int) })]
	[HarmonyPostfix]
	public static void Janitor_SetJanitorBoost_Postfix(Janitor __instance, int boostLevel)
	{
		try
		{
			JanitorLogic.ApplyRapidity(__instance, boostLevel);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor boost failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Janitor), "get_CleaningDuration")]
	[HarmonyPriority(Priority.First)]
	[HarmonyPrefix]
	public static bool Janitor_GetCleaningDuration_Prefix(Janitor __instance, ref float __result)
	{
		try
		{
			__result = JanitorLogic.GetCleanDuration(__instance);
			return false;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor cleaning duration failed, using vanilla: {ex.Message}");
			return true;
		}
	}

	[HarmonyPatch(typeof(Janitor), "FinishCleaningMopRoutine")]
	[HarmonyPostfix]
	public static void Janitor_FinishCleaningMopRoutine_Postfix(Janitor __instance)
	{
		JanitorLogic.GiveCleaningExp(__instance, 2, "mop");
	}

	[HarmonyPatch(typeof(Janitor), "FinishCleaningMopRoutine_Network")]
	[HarmonyPostfix]
	public static void Janitor_FinishCleaningMopRoutine_Network_Postfix(Janitor __instance)
	{
		JanitorLogic.GiveCleaningExp(__instance, 2, "mop-network");
	}

	[HarmonyPatch(typeof(Janitor), "FinishCleaningDustRoutine")]
	[HarmonyPostfix]
	public static void Janitor_FinishCleaningDustRoutine_Postfix(Janitor __instance)
	{
		JanitorLogic.GiveCleaningExp(__instance, 2, "dust");
	}

	[HarmonyPatch(typeof(NetworkJanitor), "FinishMopCleaning_Broadcast")]
	[HarmonyPostfix]
	public static void NetworkJanitor_FinishMopCleaning_Broadcast_Postfix(NetworkJanitor __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			JanitorLogic.GiveCleaningExp(__instance.Janitor, 2, "mop-broadcast");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor mop-broadcast XP failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(NetworkJanitor), "FinishDustCleaning_Broadcast")]
	[HarmonyPostfix]
	public static void NetworkJanitor_FinishDustCleaning_Broadcast_Postfix(NetworkJanitor __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			JanitorLogic.GiveCleaningExp(__instance.Janitor, 2, "dust-broadcast");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Janitor dust-broadcast XP failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(CleanGarbageAction), "OnExecute")]
	[HarmonyPostfix]
	public static void CleanGarbageAction_OnExecute_Postfix(CleanGarbageAction __instance)
	{
		JanitorLogic.OnGarbageCleaned(__instance);
	}

	[HarmonyPatch(typeof(TrashBag), nameof(TrashBag.JanitorMoveToTrashBag))]
	[HarmonyPostfix]
	public static void TrashBag_JanitorMoveToTrashBag_Postfix(TrashBag __instance, Garbage garbage)
	{
		JanitorLogic.OnGarbagePickedUp(__instance);
	}
}
