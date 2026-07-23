using System;
using Photon.Pun;
using UnityEngine;

namespace MultiBoxCarry;

internal static class CoopPlayer
{
	internal static bool InMultiplayer
	{
		get
		{
			try
			{
				return PhotonNetwork.InRoom;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static bool IsLocalInteraction(PlayerInteraction interaction)
	{
		if ((Object)(object)interaction == (Object)null)
		{
			return false;
		}

		try
		{
			NetworkPlayer networkPlayer = ((Component)interaction).GetComponent<NetworkPlayer>()
				?? ((Component)interaction).GetComponentInParent<NetworkPlayer>();
			if ((Object)(object)networkPlayer != (Object)null)
			{
				return networkPlayer.IsLocalPlayer || networkPlayer.m_IsLocalPlayer;
			}

			PlayerInstance instance = ((Component)interaction).GetComponent<PlayerInstance>()
				?? ((Component)interaction).GetComponentInParent<PlayerInstance>();
			if ((Object)(object)instance != (Object)null)
			{
				return instance.IsLocalPlayerInstance;
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopPlayer] Local check failed: " + ex.Message));
		}

		return !InMultiplayer;
	}

	internal static PlayerInstance GetLocalPlayerInstance()
	{
		try
		{
			PlayerManager manager = Object.FindObjectOfType<PlayerManager>();
			if ((Object)(object)manager != (Object)null && (Object)(object)manager.LocalPlayer != (Object)null)
			{
				return manager.LocalPlayer;
			}
		}
		catch
		{
		}

		try
		{
			PlayerInstance[] instances = Object.FindObjectsOfType<PlayerInstance>();
			if (instances == null)
			{
				return null;
			}

			for (int i = 0; i < instances.Length; i++)
			{
				PlayerInstance instance = instances[i];
				if ((Object)(object)instance != (Object)null && instance.IsLocalPlayerInstance)
				{
					return instance;
				}
			}
		}
		catch
		{
		}

		return null;
	}
}
