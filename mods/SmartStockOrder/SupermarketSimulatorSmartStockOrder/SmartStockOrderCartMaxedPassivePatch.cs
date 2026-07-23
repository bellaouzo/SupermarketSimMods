using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(MarketShoppingCart), "CartMaxedPassive")]
internal static class SmartStockOrderCartMaxedPassivePatch
{
	private static bool Prefix(MarketShoppingCart __instance, ref bool __result)
	{
		if (!SmartStockOrderRuntime.ShouldRemoveCartLimit())
		{
			return true;
		}
		SmartStockOrderRuntime.ApplyCartLimitOverride(__instance);
		__result = false;
		return false;
	}
}
