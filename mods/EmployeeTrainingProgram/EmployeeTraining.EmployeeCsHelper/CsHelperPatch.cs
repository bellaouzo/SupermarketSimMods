using System;
using HarmonyLib;
using Il2CppList = Il2CppSystem.Collections.Generic.List<CustomerHelper>;

namespace EmployeeTraining.EmployeeCsHelper;

[HarmonyPatch]
public static class CsHelperPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "SpawnCustomerHelper")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnCustomerHelper_Postfix(EmployeeManager __instance, int customerHelperID)
	{
		Il2CppList hired = __instance.hiredCustomerHelpers;
		Il2CppList active = __instance.ActiveCustomerHelpers ?? __instance.m_ActiveCustomerHelpers;
		CsHelperSkillManager.Instance.Spawn(hired ?? active, customerHelperID);
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireCustomerHelper")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireCustomerHepler_Postfix(EmployeeManager __instance, int customerHelperID)
	{
		CsHelperSkillManager.Instance.Fire(customerHelperID);
	}

	[HarmonyPatch(typeof(EmployeeManager), "DespawnCustomerHelper")]
	[HarmonyPrefix]
	public static void CheckoutManager_RemoveCustomerHelper_Prefix(EmployeeManager __instance, int customerHelperID)
	{
		Il2CppList active = __instance.ActiveCustomerHelpers ?? __instance.m_ActiveCustomerHelpers;
		if (active == null)
		{
			return;
		}
		CustomerHelper match = null;
		foreach (CustomerHelper helper in active)
		{
			if (helper != null && helper.CustomerHelperID == customerHelperID)
			{
				match = helper;
				break;
			}
		}
		if (match != null)
		{
			CsHelperSkillManager.Instance.Despawn(match);
		}
	}

	[HarmonyPatch(typeof(SelfCheckout), "StartCustomerHelperCheckout")]
	[HarmonyPostfix]
	public static void SelfCheckout_StartCustomerHelperCheckout_Postfix(SelfCheckout __instance)
	{
		try
		{
			CustomerHelper helper = __instance?.ControlledBy;
			if (helper != null)
			{
				CsHelperLogic.ApplyRapidity(helper);
				CsHelperLogic.WatchHelpAndGrantExp(__instance, helper);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Customer-helper checkout watch failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(CustomerHelper), "SetCustomerHelperBoost", new Type[] { typeof(int) })]
	[HarmonyPostfix]
	public static void CustomerHelper_SetCustomerHelperBoost_Postfix(CustomerHelper __instance, int boostLevel)
	{
		try
		{
			CsHelperLogic.ApplyRapidity(__instance, boostLevel);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"CustomerHelper boost failed: {ex.Message}");
		}
	}
}

[HarmonyPatch]
public static class CsHelperOrderPatch
{
	[HarmonyPatch(typeof(SelfCheckout), "StartCustomerHelperCheckoutOrder")]
	[HarmonyPostfix]
	public static void SelfCheckout_StartCustomerHelperCheckoutOrder_Postfix(SelfCheckout __instance, CustomerHelper helper)
	{
		try
		{
			if (__instance == null || helper == null)
			{
				return;
			}
			CsHelperLogic.ApplyRapidity(helper);
			CsHelperLogic.WatchHelpAndGrantExp(__instance, helper);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Customer-helper order watch failed: {ex.Message}");
		}
	}
}
