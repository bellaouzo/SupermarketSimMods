using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(TabletDevice), "OnEnable")]
internal static class SmartStockOrderTabletEnablePatch
{
	private static void Postfix(TabletDevice __instance)
	{
		SmartStockOrderRuntime.SetTabletActive(__instance, active: true);
	}
}
