using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(TabletDevice), "OnScroll")]
internal static class SmartStockOrderTabletScrollPatch
{
	private static void Postfix(TabletDevice __instance)
	{
		SmartStockOrderRuntime.SetTabletActive(__instance, active: true);
		SmartStockOrderRuntime.SetTabletVisible(__instance, visible: true);
	}
}
