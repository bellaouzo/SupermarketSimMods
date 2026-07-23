using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(ScannerDevice), "Animate")]
internal static class SmartStockOrderScannerAnimatePatch
{
	private static void Postfix(ScannerDevice __instance, bool __0)
	{
		SmartStockOrderRuntime.SetScannerVisible(__instance, __0);
	}
}
