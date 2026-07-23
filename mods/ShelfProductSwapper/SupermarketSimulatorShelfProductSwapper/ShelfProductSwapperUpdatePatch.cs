using HarmonyLib;

namespace SupermarketSimulatorShelfProductSwapper;

[HarmonyPatch(typeof(FurniturePlacer), "Update")]
internal static class ShelfProductSwapperUpdatePatch
{
	private static void Postfix()
	{
		ShelfProductSwapperRuntime.HandleHotkey();
	}
}
