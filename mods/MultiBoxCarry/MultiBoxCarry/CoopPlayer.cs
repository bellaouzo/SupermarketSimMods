using Photon.Pun;
using UnityEngine;

namespace MultiBoxCarry;

internal static class CoopPlayer
{
	private static PlayerInteraction _cachedLocal;
	private static float _nextLocalLookup;
	private static bool _hasCachedLocal;

	internal static PlayerInteraction CachedLocal => _cachedLocal;

	internal static bool HasCachedLocal =>
		_hasCachedLocal && (Object)(object)_cachedLocal != (Object)null;

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

	internal static void NoteLocal(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		_cachedLocal = player;
		_hasCachedLocal = true;
		_nextLocalLookup = Time.unscaledTime + 30f;
	}

	internal static void ClearLocalCache()
	{
		_cachedLocal = null;
		_hasCachedLocal = false;
		_nextLocalLookup = 0f;
	}

	internal static bool IsLocal(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}

		if (HasCachedLocal && (Object)(object)_cachedLocal == (Object)(object)player)
		{
			return true;
		}

		if (!InMultiplayer)
		{
			NoteLocal(player);
			return true;
		}

		NetworkPlayer networkPlayer = ((Component)player).GetComponentInParent<NetworkPlayer>();
		if ((Object)(object)networkPlayer != (Object)null)
		{
			bool local = networkPlayer.IsLocalPlayer || networkPlayer.m_IsLocalPlayer;
			if (local)
			{
				NoteLocal(player);
			}

			return local;
		}

		PlayerInstance instance = ((Component)player).GetComponentInParent<PlayerInstance>();
		if ((Object)(object)instance != (Object)null)
		{
			bool local = instance.IsLocalPlayerInstance;
			if (local)
			{
				NoteLocal(player);
			}

			return local;
		}

		return false;
	}

	internal static PlayerInteraction GetLocalPlayerInteraction()
	{
		if (HasCachedLocal)
		{
			return _cachedLocal;
		}

		if (Time.unscaledTime < _nextLocalLookup)
		{
			return null;
		}

		_nextLocalLookup = Time.unscaledTime + 2f;
		PlayerInteraction[] players = Object.FindObjectsOfType<PlayerInteraction>();
		if (players == null || players.Length == 0)
		{
			ClearLocalCache();
			return null;
		}

		if (!InMultiplayer)
		{
			NoteLocal(players[0]);
			return _cachedLocal;
		}

		foreach (PlayerInteraction player in players)
		{
			if (IsLocal(player))
			{
				return _cachedLocal;
			}
		}

		ClearLocalCache();
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
