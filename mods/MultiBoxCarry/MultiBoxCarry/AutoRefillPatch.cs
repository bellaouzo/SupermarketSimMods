using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "Update")]
internal static class AutoRefillPatch
{
	private static PlayerInteraction _cachedPlayer;
	private static PlayerObjectHolder _cachedHolder;

	private static void Postfix(PlayerInteraction __instance)
	{
		try
		{
			if (BoxInventoryController.SuppressAutoRefill || (Object)(object)__instance == (Object)null)
			{
				return;
			}

			if (!CoopPlayer.IsLocal(__instance))
			{
				return;
			}

			BoxInventory inventory = PlayerInventoryManager.GetInventory(__instance);
			if (inventory == null || inventory.IsEmpty)
			{
				return;
			}

			PlayerObjectHolder holder = GetHolder(__instance);
			if ((Object)(object)holder == (Object)null || (Object)(object)holder.CurrentObject != (Object)null)
			{
				return;
			}

			BoxInventoryController.TryPromoteNextBox(__instance);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[AutoRefillPatch] " + ex));
		}
	}

	private static PlayerObjectHolder GetHolder(PlayerInteraction player)
	{
		if ((Object)(object)_cachedPlayer == (Object)(object)player && (Object)(object)_cachedHolder != (Object)null)
		{
			return _cachedHolder;
		}

		_cachedPlayer = player;
		_cachedHolder = ((Component)player).GetComponent<PlayerObjectHolder>();
		return _cachedHolder;
	}
}
