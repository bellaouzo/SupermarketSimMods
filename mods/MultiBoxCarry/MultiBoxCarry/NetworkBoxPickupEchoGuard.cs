using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

// NetworkBox.PickUp_Broadcast is sent via server and echoes back to the sender;
// vanilla applies its own pickups through that echo. When we broadcast occupy for
// a QUEUED box (MarkQueued), the echo re-binds that box into the local hand a few
// frames later, stomping the queue state (hand/queue ping-pong). Suppress the
// vanilla RPC body for boxes that sit in the local queue; the held-box echo is
// allowed through so force-handed boxes become fully vanilla-registered.
[HarmonyPatch(typeof(NetworkBox), nameof(NetworkBox.PickUP_RPC))]
internal static class NetworkBox_PickUpRpc_QueueGuard
{
	private static bool Prefix(NetworkBox __instance, string userID)
	{
		try
		{
			if (!CoopPlayer.InMultiplayer || (Object)(object)__instance == (Object)null)
			{
				return true;
			}

			Box box = __instance.Box;
			if ((Object)(object)box == (Object)null)
			{
				box = (Object)(object)__instance.m_Box != (Object)null ? __instance.m_Box : null;
			}

			if ((Object)(object)box == (Object)null)
			{
				return true;
			}

			PlayerInteraction local = CoopPlayer.GetLocalPlayerInteraction();
			if ((Object)(object)local == (Object)null)
			{
				return true;
			}

			BoxInventory inventory = PlayerInventoryManager.GetInventory(local);
			if (inventory == null || inventory.IsEmpty)
			{
				return true;
			}

			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				IQueuableBox queued = inventory.QueuedBoxes[i];
				if (queued != null && BoxUtility.SameBox(queued.Raw, box))
				{
					Plugin.Log.LogInfo((object)("[MultiBox][dbg] Suppressed PickUP_RPC echo for queued box "
						+ BoxUtility.Describe(box) + " (userID=" + userID + ")"));
					__instance.IsNetworkOccupied = true;
					return false;
				}
			}

			return true;
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[MultiBox] PickUP_RPC guard failed: " + ex.Message));
			return true;
		}
	}
}
