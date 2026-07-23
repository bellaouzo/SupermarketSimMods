using Photon.Pun;
using UnityEngine;

namespace MultiBoxCarry;

internal static class CoopPlayer
{
	private static PlayerInteraction _cachedLocal;
	private static float _nextLocalLookup;

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

	internal static bool IsLocal(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}

		if (!InMultiplayer)
		{
			return true;
		}

		NetworkPlayer networkPlayer = ((Component)player).GetComponentInParent<NetworkPlayer>();
		if ((Object)(object)networkPlayer != (Object)null)
		{
			return networkPlayer.IsLocalPlayer || networkPlayer.m_IsLocalPlayer;
		}

		PlayerInstance instance = ((Component)player).GetComponentInParent<PlayerInstance>();
		if ((Object)(object)instance != (Object)null)
		{
			return instance.IsLocalPlayerInstance;
		}

		return true;
	}

	internal static PlayerInteraction GetLocalPlayerInteraction()
	{
		if ((Object)(object)_cachedLocal != (Object)null && Time.unscaledTime < _nextLocalLookup)
		{
			return _cachedLocal;
		}

		_nextLocalLookup = Time.unscaledTime + 1f;
		PlayerInteraction[] players = Object.FindObjectsOfType<PlayerInteraction>();
		if (players == null || players.Length == 0)
		{
			_cachedLocal = null;
			return null;
		}

		if (!InMultiplayer)
		{
			_cachedLocal = players[0];
			return _cachedLocal;
		}

		foreach (PlayerInteraction player in players)
		{
			if (IsLocal(player))
			{
				_cachedLocal = player;
				return _cachedLocal;
			}
		}

		_cachedLocal = null;
		return null;
	}

	internal static PlayerInstance GetLocalPlayerInstance()
	{
		PlayerInteraction local = GetLocalPlayerInteraction();
		if ((Object)(object)local == (Object)null)
		{
			return null;
		}

		return ((Component)local).GetComponentInParent<PlayerInstance>();
	}

	internal static PlayerObjectHolder GetLocalHolder()
	{
		PlayerInteraction local = GetLocalPlayerInteraction();
		return (Object)(object)local == (Object)null
			? null
			: ((Component)local).GetComponent<PlayerObjectHolder>();
	}
}
