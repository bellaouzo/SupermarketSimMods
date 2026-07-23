using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(Box), nameof(Box.OpenBox), new Type[] { })]
internal static class Box_OpenBox_Patch
{
	[HarmonyPrefix]
	private static void Prefix(Box __instance)
	{
		try
		{
			BoxInventoryController.SuppressAutoRefill = true;
			OnThrowMessanger.WriteMessage("box");
			if ((Object)(object)__instance != (Object)null)
			{
				NetworkBoxUtil.EnsureOwnership(new BoxAdapter(__instance));
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[Box_OpenBox_Patch] " + ex));
		}
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		BoxInventoryController.SuppressAutoRefill = false;
	}
}

[HarmonyPatch(typeof(Box), nameof(Box.OpenBox), new Type[] { typeof(PlayerInstance) })]
internal static class Box_OpenBox_Player_Patch
{
	[HarmonyPrefix]
	private static void Prefix(Box __instance)
	{
		try
		{
			BoxInventoryController.SuppressAutoRefill = true;
			OnThrowMessanger.WriteMessage("box");
			if ((Object)(object)__instance != (Object)null)
			{
				NetworkBoxUtil.EnsureOwnership(new BoxAdapter(__instance));
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[Box_OpenBox_Player_Patch] " + ex));
		}
	}

	[HarmonyPostfix]
	private static void Postfix()
	{
		BoxInventoryController.SuppressAutoRefill = false;
	}
}
