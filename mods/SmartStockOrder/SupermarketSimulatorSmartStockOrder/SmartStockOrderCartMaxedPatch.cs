using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(MarketShoppingCart), "CartMaxed")]
internal static class SmartStockOrderCartMaxedPatch
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
