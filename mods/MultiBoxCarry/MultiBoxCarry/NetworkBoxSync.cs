using System;
using Photon.Pun;
using UnityEngine;

namespace MultiBoxCarry;

internal static class NetworkBoxSync
{
	internal static NetworkBox GetNetworkBox(IQueuableBox box)
	{
		if (box?.Raw == null)
		{
			return null;
		}

		Component component = box.Raw as Component;
		if ((Object)(object)component == (Object)null)
		{
			return null;
		}

		return ((Component)component).GetComponent<NetworkBox>()
			?? ((Component)component).GetComponentInParent<NetworkBox>()
			?? ((Component)component).GetComponentInChildren<NetworkBox>();
	}

	internal static bool IsNetworkOccupied(IQueuableBox box)
	{
		NetworkBox networkBox = GetNetworkBox(box);
		return (Object)(object)networkBox != (Object)null && networkBox.IsNetworkOccupied;
	}

	internal static void MarkQueued(IQueuableBox box)
	{
		SetNetworkOccupied(box, occupied: true, CoopNetwork.StateQueued);
	}

	internal static void MarkHeld(IQueuableBox box)
	{
		SetNetworkOccupied(box, occupied: true, CoopNetwork.StateHeld);
	}

	internal static void MarkReleased(IQueuableBox box)
	{
		SetNetworkOccupied(box, occupied: false, CoopNetwork.StateFree);
	}

	internal static void ApplyRemoteOccupy(int viewId, bool occupied)
	{
		if (viewId <= 0)
		{
			return;
		}

		try
		{
			PhotonView view = PhotonView.Find(viewId);
			if ((Object)(object)view == (Object)null)
			{
				return;
			}

			NetworkBox networkBox = ((Component)view).GetComponent<NetworkBox>()
				?? ((Component)view).GetComponentInChildren<NetworkBox>()
				?? ((Component)view).GetComponentInParent<NetworkBox>();
			if ((Object)(object)networkBox == (Object)null)
			{
				return;
			}

			networkBox.IsNetworkOccupied = occupied;
			Box gameBox = networkBox.Box ?? networkBox.m_Box;
			if ((Object)(object)gameBox != (Object)null)
			{
				Transform owner = occupied ? ((Component)networkBox).transform : null;
				gameBox.SetOccupy(occupied, owner);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxSync] ApplyRemoteOccupy failed: " + ex.Message));
		}
	}

	private static void SetNetworkOccupied(IQueuableBox box, bool occupied, string state)
	{
		if (box == null)
		{
			return;
		}

		try
		{
			NetworkBox networkBox = GetNetworkBox(box);
			if ((Object)(object)networkBox == (Object)null)
			{
				return;
			}

			EnsureOwnership(networkBox);
			networkBox.IsNetworkOccupied = occupied;

			if (CoopPlayer.InMultiplayer)
			{
				CoopNetwork.BroadcastOccupy(networkBox.ViewId, state);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxSync] SetNetworkOccupied failed: " + ex.Message));
		}
	}

	private static void EnsureOwnership(NetworkBox networkBox)
	{
		if (!CoopPlayer.InMultiplayer)
		{
			return;
		}

		PhotonView view = networkBox.PhotonView ?? networkBox.m_PhotonView;
		if ((Object)(object)view == (Object)null || view.IsMine)
		{
			return;
		}

		try
		{
			view.RequestOwnership();
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[NetworkBoxSync] RequestOwnership failed: " + ex.Message));
		}
	}
}
