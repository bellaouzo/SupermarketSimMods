using HarmonyLib;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FurnitureBoxInteraction), "OnThrow")]
internal static class FurnitureBoxInteraction_OnThrow_Patch
{
	private static bool Prefix(bool isDown)
	{
		return isDown;
	}
}
