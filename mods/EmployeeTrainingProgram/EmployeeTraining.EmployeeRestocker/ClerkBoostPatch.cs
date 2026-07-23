using System;
using HarmonyLib;
using SupermarketSimulator.Clerk;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch]
public static class ClerkBoostPatch
{
	[HarmonyPatch(typeof(Clerk), "SetRestockerBoost")]
	[HarmonyPostfix]
	public static void Clerk_SetRestockerBoost_Postfix(Clerk __instance, int boostLevel)
	{
		try
		{
			ClerkLogic.ApplyRapidity(__instance, boostLevel);
			Plugin.LogInfo($"Clerk[{__instance?.EmployeeId}] boost={boostLevel} stacked with training.");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Clerk boost stacking failed: " + ex.Message);
		}
	}
}
