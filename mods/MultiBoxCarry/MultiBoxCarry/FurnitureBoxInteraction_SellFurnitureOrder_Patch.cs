using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(FurnitureBoxInteraction), "SellFurnitureOrder")]
internal static class FurnitureBoxInteraction_SellFurnitureOrder_Patch
{
	[HarmonyPostfix]
	private static void Postfix(FurnitureBoxInteraction __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}

			PlayerInteraction player = ((Component)__instance).GetComponent<PlayerInteraction>();
			if (!CoopPlayer.IsLocal(player))
			{
				return;
			}

			PlayerObjectHolder component = ((Component)__instance).GetComponent<PlayerObjectHolder>();
			if ((Object)(object)component != (Object)null)
			{
				component.SetNullCurrentObject();
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[FurnitureBoxSellResolvePatch.SellFurnitureOrder] " + ex));
		}
	}
}
