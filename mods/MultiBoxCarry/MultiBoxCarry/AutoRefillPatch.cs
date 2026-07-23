using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "Update")]
internal static class AutoRefillPatch
{
	private static PlayerInteraction _cachedPlayer;
	private static PlayerObjectHolder _cachedHolder;
	private static float _nextHandshakeAt;

	private static void Postfix(PlayerInteraction __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}

			if (CoopPlayer.HasCachedLocal
				&& (Object)(object)__instance != (Object)(object)CoopPlayer.CachedLocal)
			{
				return;
			}

			if (!CoopPlayer.IsLocal(__instance))
			{
				return;
			}

			float now = Time.unscaledTime;
			if (now >= _nextHandshakeAt)
			{
				_nextHandshakeAt = now + 4f;
				CoopHandshake.Tick();
			}

			// These two repair loops are load-bearing at per-frame cadence: recovery
			// must run before the promote check below (throttling it let promote fire
			// into a desynced hand and duplicate boxes into the queue), and the
			// hidden-renderer re-show keeps a promoted box visible.
			BoxInventoryController.RecoverDesyncedHold(__instance);

			PlayerObjectHolder holder = GetHolder(__instance);
			IQueuableBox held = BoxUtility.GetHeldQueueBox(holder);
			if (held != null && BoxUtility.HasHiddenRenderer(held))
			{
				BoxUtility.EnsurePresented(held);
			}

			BoxInventory inventory = PlayerInventoryManager.GetInventory(__instance);
			if (inventory == null || inventory.IsEmpty)
			{
				return;
			}

			HandHighlightGuard.Tick(__instance);

			if (BoxInventoryController.SuppressAutoRefill)
			{
				return;
			}

			if (BoxUtility.IsInPlacingMode(__instance))
			{
				return;
			}

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
