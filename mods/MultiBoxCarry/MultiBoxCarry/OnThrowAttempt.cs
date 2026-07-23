using System;
using HarmonyLib;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerObjectHolder), "TryThrowObject")]
internal static class OnThrowAttempt
{
	[HarmonyPostfix]
	public static void Postfix(bool __result)
	{
		try
		{
			if (__result)
			{
				OnThrowMessanger.WriteMessage("throw");
			}
		}
		catch (Exception)
		{
		}
	}
}
