using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(MarketShoppingCart), "Start")]
internal static class SmartStockOrderCartStartPatch
{
	private static void Postfix(MarketShoppingCart __instance)
	{
		SmartStockOrderRuntime.ApplyCartLimitOverride(__instance);
	}
}
