using System;
using HarmonyLib;
using UnityEngine;
using __Project__.Scripts.Interaction;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "InteractionEnd")]
internal static class OnThrowSuccess
{
	[HarmonyPostfix]
	private static void Postfix(PlayerInteraction __instance, Interaction interaction)
	{
		try
		{
			if (!OnThrowMessanger.hasThrowMessage
				|| (Object)(object)__instance == (Object)null
				|| (Object)(object)interaction == (Object)null
				|| !(interaction is BoxInteraction || interaction is FurnitureBoxInteraction || interaction is FloorBoxInteraction))
			{
				return;
			}

			if (!CoopPlayer.IsLocal(__instance))
			{
				return;
			}

			PlayerObjectHolder component = ((Component)__instance).GetComponent<PlayerObjectHolder>();
			if ((Object)(object)component != (Object)null)
			{
				component.SetNullCurrentObject();
			}

			BoxInteraction boxInteraction = ((Component)__instance).GetComponent<BoxInteraction>();
			if ((Object)(object)boxInteraction != (Object)null)
			{
				boxInteraction.m_Box = null;
				boxInteraction.m_PlacingMode = false;
			}

			BoxInventoryController.PruneDestroyedQueued(__instance);
			if (!BoxUtility.IsInPlacingMode(__instance))
			{
				BoxInventoryController.EnsureHandOrPromotePublic(__instance);
			}

			OnThrowMessanger.GaveMessage("throw");
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[InteractionEndPatch] Error: " + ex));
		}
	}
}
