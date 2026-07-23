using HarmonyLib;

namespace SupermarketSimulatorFurnitureAligner;

[HarmonyPatch(typeof(FurniturePlacer), "CheckPlaceFurniture")]
internal static class FurniturePlacerCheckPlacePatch
{
	private static void Postfix(ref bool __result)
	{
		if (CoopPlacement.AllowOutsideBypass)
		{
			__result = true;
		}
	}
}
