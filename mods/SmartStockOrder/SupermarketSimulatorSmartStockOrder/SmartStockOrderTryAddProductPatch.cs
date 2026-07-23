using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(MarketShoppingCart), "TryAddProduct")]
internal static class SmartStockOrderTryAddProductPatch
{
	private static bool Prefix(MarketShoppingCart __instance, ItemQuantity salesItem, SalesType salesType, ref bool __result)
	{
		if (!SmartStockOrderRuntime.ShouldRemoveCartLimit())
		{
			return true;
		}
		SmartStockOrderRuntime.ApplyCartLimitOverride(__instance);
		return true;
	}
}
