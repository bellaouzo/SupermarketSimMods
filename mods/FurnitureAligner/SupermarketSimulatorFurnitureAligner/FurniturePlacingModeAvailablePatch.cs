using HarmonyLib;

namespace SupermarketSimulatorFurnitureAligner;

[HarmonyPatch(typeof(FurniturePlacingMode), "get_AvailablePosition")]
internal static class FurniturePlacingModeAvailablePatch
{
	private static void Postfix(ref bool __result)
	{
		if (CoopPlacement.AllowOutsideBypass)
		{
			__result = true;
		}
	}
}
