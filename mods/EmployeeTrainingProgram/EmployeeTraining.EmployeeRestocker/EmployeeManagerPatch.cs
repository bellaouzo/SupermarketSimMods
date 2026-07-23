using System;
using HarmonyLib;
using SupermarketSimulator.Clerk;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch]
public static class EmployeeManagerPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "HireRestocker")]
	[HarmonyPostfix]
	public static void EmployeeManager_HireRestocker_Postfix(int restockerID)
	{
		try
		{
			RestockerSkillManager.Instance.Register(restockerID);
			Plugin.LogInfo($"Registered hired restocker id={restockerID}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"HireRestocker register failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "SpawnRestocker")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnRestocker_Postfix(EmployeeManager __instance, int restockerID)
	{
		try
		{
			Clerk clerk = __instance.GetRestockerByID(restockerID);
			if (clerk != null)
			{
				RestockerSkillManager.Instance.AssignClerk(clerk);
				Plugin.LogInfo($"Spawned restocker/clerk id={clerk.EmployeeId}");
			}
			else
			{
				RestockerSkillManager.Instance.Register(restockerID);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"SpawnRestocker register failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "SpawnRestockerNetwork")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnRestockerNetwork_Postfix(Clerk restocker)
	{
		if (restocker == null)
		{
			return;
		}
		try
		{
			RestockerSkillManager.Instance.AssignClerk(restocker);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"SpawnRestockerNetwork register failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireRestocker")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireRestocker_Postfix(int restockerID)
	{
		try
		{
			RestockerSkillManager.Instance.Fire(restockerID);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"FireRestocker failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "DespawnRestocker")]
	[HarmonyPrefix]
	public static void EmployeeManager_DespawnRestocker_Prefix(int restockerID)
	{
		try
		{
			RestockerSkill skill = RestockerSkillManager.Instance.GetSkillById(restockerID);
			skill?.Despawn();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"DespawnRestocker cleanup failed: {ex.Message}");
		}
	}
}
