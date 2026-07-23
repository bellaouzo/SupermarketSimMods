using HarmonyLib;
using UnityEngine;

namespace SupermarketSimulatorFurnitureAligner;

[HarmonyPatch(typeof(FurniturePlacer), "Update")]
internal static class FurniturePlacerUpdatePatch
{
	private static void Postfix(FurniturePlacer __instance)
	{
		FurniturePlacingMode currentPlacingMode = __instance.CurrentPlacingMode;
		Transform furniture = ((Object)(object)currentPlacingMode != (Object)null) ? currentPlacingMode.Furniture : null;
		FurnitureAlignerRuntime.OnPlacerUpdate(__instance.PlacingMode, furniture);
	}
}
