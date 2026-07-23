using System;
using HarmonyLib;
using Il2CppList = Il2CppSystem.Collections.Generic.List<IceCreamHelper>;

namespace EmployeeTraining.EmployeeIceCream;

[HarmonyPatch]
public static class IceCreamHelperPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "HireIceCreamHelper")]
	[HarmonyPostfix]
	public static void EmployeeManager_HireIceCreamHelper_Postfix(int id, float hiringCost)
	{
		try
		{
			IceCreamHelperSkillManager.Instance.Register(id);
			Plugin.LogInfo($"Registered hired ice cream helper id={id}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"HireIceCreamHelper register failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireIceCreamHelper")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireIceCreamHelper_Postfix(int id)
	{
		try
		{
			IceCreamHelperSkillManager.Instance.Fire(id);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"FireIceCreamHelper failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "SpawnIceCreamHelper")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnIceCreamHelper_Postfix(EmployeeManager __instance, int id)
	{
		try
		{
			Il2CppList active = __instance.m_ActiveIceCreamHelpers;
			if (active != null)
			{
				IceCreamHelperSkillManager.Instance.Spawn(active, id);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"SpawnIceCreamHelper assign failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "DespawnIceCreamHelper")]
	[HarmonyPrefix]
	public static void EmployeeManager_DespawnIceCreamHelper_Prefix(EmployeeManager __instance, int id)
	{
		try
		{
			Il2CppList active = __instance.m_ActiveIceCreamHelpers;
			if (active == null)
			{
				return;
			}
			IceCreamHelper match = null;
			foreach (IceCreamHelper helper in active)
			{
				if (helper != null && helper.ID == id)
				{
					match = helper;
					break;
				}
			}
			if (match != null)
			{
				IceCreamHelperSkillManager.Instance.Despawn(match);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"DespawnIceCreamHelper cleanup failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(IceCreamHelper), "SetHelperBoost", new Type[] { typeof(int) })]
	[HarmonyPostfix]
	public static void IceCreamHelper_SetHelperBoost_Postfix(IceCreamHelper __instance, int boostLevel)
	{
		try
		{
			IceCreamHelperLogic.ApplyRapidity(__instance, boostLevel);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"IceCreamHelper boost failed: {ex.Message}");
		}
	}
}
