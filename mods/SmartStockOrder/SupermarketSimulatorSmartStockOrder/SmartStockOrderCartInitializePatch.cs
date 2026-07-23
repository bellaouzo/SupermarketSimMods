using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(MarketShoppingCart), "Initialize")]
internal static class SmartStockOrderCartInitializePatch
{
	private static void Postfix(MarketShoppingCart __instance)
	{
		SmartStockOrderRuntime.ApplyCartLimitOverride(__instance);
	}
}
