using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(TabletDevice), "OnDisable")]
internal static class SmartStockOrderTabletDisablePatch
{
	private static void Postfix(TabletDevice __instance)
	{
		SmartStockOrderRuntime.SetTabletActive(__instance, active: false);
	}
}
