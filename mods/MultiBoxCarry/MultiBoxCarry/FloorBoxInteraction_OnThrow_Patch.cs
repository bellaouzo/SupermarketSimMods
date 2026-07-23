using HarmonyLib;
using __Project__.Scripts.Interaction;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FloorBoxInteraction), "OnThrow")]
internal static class FloorBoxInteraction_OnThrow_Patch
{
	private static bool Prefix(bool isDown)
	{
		return isDown;
	}
}
