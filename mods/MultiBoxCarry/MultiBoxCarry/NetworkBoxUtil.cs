using System;
using UnityEngine;

namespace MultiBoxCarry;

internal static class NetworkBoxUtil
{
	internal static void MarkQueued(IQueuableBox queueBox)
	{
		if (queueBox == null || !CoopPlayer.InMultiplayer)
		{
			return;
		}

		try
		{
			NetworkBox networkBox = GetNetworkBox(queueBox);
			if ((Object)(object)networkBox == (Object)null)
			{
				return;
			}

			networkBox.IsNetworkOccupied = true;
			networkBox.DisableNetworkTransformSync(true);
			PlayerInstance local = CoopPlayer.GetLocalPlayerInstance();
			if ((Object)(object)local != (Object)null)
			{
				networkBox.PickUp_Broadcast(local);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] MarkQueued failed: " + ex.Message));
		}
	}

	internal static void MarkReleased(IQueuableBox queueBox)
	{
		if (queueBox == null || !CoopPlayer.InMultiplayer)
		{
			return;
		}

		try
		{
			NetworkBox networkBox = GetNetworkBox(queueBox);
			if ((Object)(object)networkBox == (Object)null)
			{
				return;
			}

			networkBox.IsNetworkOccupied = false;
			networkBox.DisableNetworkTransformSync(false);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] MarkReleased failed: " + ex.Message));
		}
	}

	private static NetworkBox GetNetworkBox(IQueuableBox queueBox)
	{
		if (queueBox?.transform == null)
		{
			return null;
		}

		return ((Component)queueBox.transform).GetComponent<NetworkBox>()
			?? ((Component)queueBox.transform).GetComponentInParent<NetworkBox>();
	}
}
