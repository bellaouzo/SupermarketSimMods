using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(TabletDevice), "Animate")]
internal static class SmartStockOrderTabletAnimatePatch
{
	private static void Postfix(TabletDevice __instance, bool __0)
	{
		SmartStockOrderRuntime.SetTabletVisible(__instance, __0);
	}
}
