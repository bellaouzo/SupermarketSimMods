using HarmonyLib;

namespace SupermarketSimulatorSmartStockOrder;

[HarmonyPatch(typeof(FurniturePlacer), "Update")]
internal static class SmartStockOrderUpdatePatch
{
	private static void Postfix()
	{
		SmartStockOrderRuntime.HandleHotkeys();
	}
}
