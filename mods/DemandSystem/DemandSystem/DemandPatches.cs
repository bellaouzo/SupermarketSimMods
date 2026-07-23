using HarmonyLib;
using UnityEngine;

namespace DemandSystem;

[HarmonyPatch(typeof(DayCycleManager), "StartDayCycle")]
internal static class DayCycleManagerStartDayCyclePatch
{
	private static void Postfix(DayCycleManager __instance)
	{
		DemandState.GenerateForDay(__instance.CurrentDay);
	}
}

[HarmonyPatch(typeof(DayCycleManager), "StartNextDay")]
internal static class DayCycleManagerStartNextDayPatch
{
	private static void Postfix(DayCycleManager __instance)
	{
		DemandState.GenerateForDay(__instance.CurrentDay);
	}
}

[HarmonyPatch(typeof(CustomerManager), "CreateShoppingList")]
internal static class CustomerManagerCreateShoppingListPatch
{
	private static void Postfix(ref ItemQuantity __result)
	{
		DemandState.ApplyToShoppingList(__result);
	}
}

[HarmonyPatch(typeof(DayCycleManager), "Update")]
internal static class DayCycleManagerUpdatePatch
{
	private static float _nextEnsure;

	private static void Postfix(DayCycleManager __instance)
	{
		if ((Object)(object)__instance == (Object)null || Time.unscaledTime < _nextEnsure)
		{
			return;
		}

		_nextEnsure = Time.unscaledTime + 1f;
		DemandState.EnsureForDay(__instance.CurrentDay);
	}
}
