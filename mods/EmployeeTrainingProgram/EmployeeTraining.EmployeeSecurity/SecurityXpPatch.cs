using System;
using HarmonyLib;
using UnityEngine;

namespace EmployeeTraining.EmployeeSecurity;

[HarmonyPatch]
public static class SecurityXpPatch
{
	[HarmonyPatch(typeof(SecurityGuard), "OnShoplifterDetected")]
	[HarmonyPostfix]
	public static void SecurityGuard_OnShoplifterDetected_Postfix(SecurityGuard __instance, Shoplifter shoplifter, bool OnAlert)
	{
		SecurityLogic.GiveDetectExp(__instance);
	}

	[HarmonyPatch(typeof(ChaseState), "OnEnter")]
	[HarmonyPostfix]
	public static void ChaseState_OnEnter_Postfix(ChaseState __instance)
	{
		try
		{
			SecurityLogic.OnShoplifterDetected(__instance);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Security chase XP failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Customer), "RunAway")]
	[HarmonyPostfix]
	public static void Customer_RunAway_Postfix(Customer __instance, bool isHitByGuard, SecurityGuard securityGuard)
	{
		SecurityLogic.OnShoplifterBeaten(__instance, isHitByGuard, securityGuard);
	}

	[HarmonyPatch(typeof(ShoplifterTutorialCustomer), "RunAway")]
	[HarmonyPostfix]
	public static void ShoplifterTutorialCustomer_RunAway_Postfix(ShoplifterTutorialCustomer __instance, bool isHitByGuard, SecurityGuard securityGuard)
	{
		SecurityLogic.OnShoplifterBeaten(null, isHitByGuard, securityGuard);
	}
}
