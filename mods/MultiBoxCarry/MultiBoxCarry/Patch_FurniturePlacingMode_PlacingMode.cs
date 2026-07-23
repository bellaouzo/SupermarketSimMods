using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FurniturePlacingMode), "PlacingMode")]
internal static class Patch_FurniturePlacingMode_PlacingMode
{
	[HarmonyPostfix]
	private static void Postfix(FurniturePlacingMode __instance, bool value)
	{
		try
		{
			if (!value && !((Object)(object)__instance == (Object)null))
			{
				PlayerObjectHolder val = CoopPlayer.GetLocalHolder();
				if (!((Object)(object)val == (Object)null) && !((Object)(object)val.CurrentObject == (Object)null))
				{
					val.SetNullCurrentObject();
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[MultiBox] FurniturePlacingMode postfix error: " + ex));
		}
	}
}
