using System;
using HarmonyLib;

namespace EmployeeTraining.EmployeeBaker;

[HarmonyPatch]
public static class BakerPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "HireBaker")]
	[HarmonyPostfix]
	public static void EmployeeManager_HireBaker_Postfix(int bakerId, float hiringCost)
	{
		try
		{
			BakerSkillManager.Instance.Register(bakerId);
			Plugin.LogInfo($"Registered hired baker id={bakerId}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"HireBaker register failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireBaker")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireBaker_Postfix(int bakerId)
	{
		try
		{
			BakerSkillManager.Instance.Fire(bakerId);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"FireBaker failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(BakeryManager), "SpawnBaker")]
	[HarmonyPostfix]
	public static void BakeryManager_SpawnBaker_Postfix(BakeryManager __instance, int bakerId)
	{
		try
		{
			Il2CppSystem.Collections.Generic.List<Baker> bakers = __instance.Bakers;
			if (bakers == null)
			{
				return;
			}
			Baker match = null;
			foreach (Baker baker in bakers)
			{
				if (baker != null && baker.BakerID == bakerId)
				{
					match = baker;
					break;
				}
			}
			if (match != null)
			{
				BakerSkillManager.Instance.AssignBaker(match);
				BakerLogic.ApplyRapidity(match);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"SpawnBaker assign failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(BakeryManager), "DespawnBaker")]
	[HarmonyPrefix]
	public static void BakeryManager_DespawnBaker_Prefix(BakeryManager __instance, int bakerId)
	{
		try
		{
			Il2CppSystem.Collections.Generic.List<Baker> bakers = __instance.Bakers;
			if (bakers == null)
			{
				return;
			}
			Baker match = null;
			foreach (Baker baker in bakers)
			{
				if (baker != null && baker.BakerID == bakerId)
				{
					match = baker;
					break;
				}
			}
			if (match != null)
			{
				BakerSkillManager.Instance.Despawn(match);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"DespawnBaker cleanup failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Baker), "SetBakerBoost", new Type[] { typeof(int) })]
	[HarmonyPostfix]
	public static void Baker_SetBakerBoost_Postfix(Baker __instance, int boostLevel)
	{
		try
		{
			BakerLogic.ApplyRapidity(__instance, boostLevel);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Baker boost failed: {ex.Message}");
		}
	}
}
