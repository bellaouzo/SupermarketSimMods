using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "Update")]
internal static class AutoRefillPatch
{
	private static void Postfix(PlayerInteraction __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null || !CoopPlayer.IsLocalInteraction(__instance))
			{
				return;
			}

			CoopNetwork.Tick();
			if (!CoopNetwork.PeersMatch || BoxInventoryController.SuppressAutoRefill)
			{
				return;
			}

			PlayerObjectHolder component = ((Component)__instance).GetComponent<PlayerObjectHolder>();
			if (!((Object)(object)component == (Object)null) && (Object)(object)component.CurrentObject == (Object)null)
			{
				BoxInventory inventory = PlayerInventoryManager.Inventory;
				if (!inventory.IsEmpty)
				{
					BoxInventoryController.TryPromoteNextBox(__instance);
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[AutoRefillPatch] " + ex));
		}
	}
}
