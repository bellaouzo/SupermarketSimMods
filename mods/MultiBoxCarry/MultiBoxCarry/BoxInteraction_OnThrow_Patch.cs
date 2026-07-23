using HarmonyLib;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(BoxInteraction), "OnThrow")]
internal static class BoxInteraction_OnThrow_Patch
{
	private static bool Prefix(bool isDown)
	{
		return isDown;
	}
}
