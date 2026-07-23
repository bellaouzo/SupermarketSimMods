using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FurnitureBoxInteraction), "SellFurniture")]
internal static class FurnitureBoxInteraction_SellFurniture_Patch
{
	[HarmonyPostfix]
	private static void Postfix(FurnitureBoxInteraction __instance)
	{
		try
		{
			if (!((Object)(object)__instance == (Object)null))
			{
				PlayerObjectHolder component = ((Component)__instance).GetComponent<PlayerObjectHolder>();
				if (!((Object)(object)component == (Object)null))
				{
					component.SetNullCurrentObject();
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[FurnitureBoxSellResolvePatch.SellFurniture] " + ex));
		}
	}
}
