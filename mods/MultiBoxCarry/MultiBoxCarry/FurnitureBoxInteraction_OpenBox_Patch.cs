using System;
using HarmonyLib;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FurnitureBoxInteraction), "OpenBox")]
internal static class FurnitureBoxInteraction_OpenBox_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		try
		{
			OnThrowMessanger.WriteMessage("box");
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[FurnitureBoxInteraction_OpenBox_Patch] " + ex));
		}
	}
}
