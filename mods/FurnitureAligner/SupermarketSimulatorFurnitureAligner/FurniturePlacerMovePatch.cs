using HarmonyLib;
using UnityEngine;

namespace SupermarketSimulatorFurnitureAligner;

[HarmonyPatch(typeof(FurniturePlacer), "MoveFurniture")]
internal static class FurniturePlacerMovePatch
{
	private static void Prefix(FurniturePlacer __instance, ref Vector3 targetPos)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (FurnitureAlignerRuntime.IsActive && __instance.PlacingMode)
		{
			FurniturePlacingMode currentPlacingMode = __instance.CurrentPlacingMode;
			if (!((Object)(object)currentPlacingMode == (Object)null) && !((Object)(object)currentPlacingMode.Furniture == (Object)null))
			{
				targetPos = FurnitureAlignerRuntime.ApplyAlignment(currentPlacingMode.Furniture, targetPos);
			}
		}
	}
}
