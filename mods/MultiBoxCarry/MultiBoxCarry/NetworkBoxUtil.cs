using System;
using Photon.Pun;
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

			EnsureOwnership(networkBox);
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

	internal static void MarkHeld(IQueuableBox queueBox)
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

			EnsureOwnership(networkBox);
			networkBox.IsNetworkOccupied = true;
			networkBox.DisableNetworkTransformSync(false);
			PlayerInstance local = CoopPlayer.GetLocalPlayerInstance();
			if ((Object)(object)local != (Object)null)
			{
				networkBox.PickUp_Broadcast(local);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] MarkHeld failed: " + ex.Message));
		}
	}

	internal static void MarkReleased(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return;
		}

		try
		{
			ClearLocalOccupy(queueBox);
			BoxUtility.PrepareBoxForWorld(queueBox);
			ClearOccupyFlags(queueBox);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] MarkReleased failed: " + ex.Message));
		}
	}

	internal static void ClearOccupyFlags(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return;
		}

		try
		{
			ClearLocalOccupy(queueBox);
			BoxUtility.EnsurePresented(queueBox);

			if (!CoopPlayer.InMultiplayer)
			{
				return;
			}

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
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] ClearOccupyFlags failed: " + ex.Message));
		}
	}

	internal static void PrepareForHandPickup(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return;
		}

		try
		{
			ClearLocalOccupy(queueBox);

			if (!CoopPlayer.InMultiplayer)
			{
				return;
			}

			NetworkBox networkBox = GetNetworkBox(queueBox);
			if ((Object)(object)networkBox == (Object)null)
			{
				return;
			}

			EnsureOwnership(networkBox);
			networkBox.IsNetworkOccupied = false;
			networkBox.DisableNetworkTransformSync(false);
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] PrepareForHandPickup failed: " + ex.Message));
		}
	}

	internal static void EnsureOwnership(IQueuableBox queueBox)
	{
		if (queueBox == null || !CoopPlayer.InMultiplayer)
		{
			return;
		}

		try
		{
			EnsureOwnership(GetNetworkBox(queueBox));
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxUtil] EnsureOwnership failed: " + ex.Message));
		}
	}

	private static void EnsureOwnership(NetworkBox networkBox)
	{
		if ((Object)(object)networkBox == (Object)null)
		{
			return;
		}

		PhotonView view = networkBox.PhotonView;
		if ((Object)(object)view == (Object)null)
		{
			view = networkBox.m_PhotonView;
		}

		if ((Object)(object)view != (Object)null && !view.IsMine)
		{
			view.RequestOwnership();
		}
	}

	internal static void ReleaseAllQueued(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return;
		}

		for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
		{
			MarkReleased(inventory.QueuedBoxes[i]);
		}
	}

	internal static void FlushOnLeave()
	{
		PlayerInventoryManager.FlushAll();
	}

	private static void ClearLocalOccupy(IQueuableBox queueBox)
	{
		if (queueBox is not BoxAdapter adapter)
		{
			return;
		}

		Box box = adapter.GetBox();
		if ((Object)(object)box == (Object)null)
		{
			return;
		}

		Transform occupyOwner = box.OccupyOwner;
		if ((Object)(object)occupyOwner != (Object)null)
		{
			box.SetOccupy(false, occupyOwner);
		}
	}

	private static NetworkBox GetNetworkBox(IQueuableBox queueBox)
	{
		if (queueBox?.transform == null)
		{
			return null;
		}

		try
		{
			Transform transform = queueBox.transform;
			if ((Object)(object)transform == (Object)null)
			{
				return null;
			}

			return ((Component)transform).GetComponent<NetworkBox>()
				?? ((Component)transform).GetComponentInParent<NetworkBox>();
		}
		catch
		{
			return null;
		}
	}
}
