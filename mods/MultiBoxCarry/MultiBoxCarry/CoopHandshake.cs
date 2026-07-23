using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace MultiBoxCarry;

internal static class CoopHandshake
{
	private const string HandshakeKey = "mbc_hs";

	private static bool _wasInRoom;
	private static bool _peersMatch = true;
	private static string _lastHandshakeWarn = string.Empty;

	internal static bool PeersMatch => !CoopPlayer.InMultiplayer || _peersMatch;

	internal static bool IsHost
	{
		get
		{
			if (!CoopPlayer.InMultiplayer)
			{
				return true;
			}

			try
			{
				return PhotonNetwork.IsMasterClient;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static void Tick()
	{
		bool inRoom = CoopPlayer.InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			if (IsHost)
			{
				PublishHandshake();
			}
			else
			{
				UpdatePeersMatch();
			}
		}
		else if (!inRoom && _wasInRoom)
		{
			NetworkBoxUtil.FlushOnLeave();
			_peersMatch = true;
			_lastHandshakeWarn = string.Empty;
		}

		_wasInRoom = inRoom;
		if (!inRoom)
		{
			return;
		}

		if (IsHost)
		{
			PublishHandshake();
			_peersMatch = true;
		}
		else
		{
			UpdatePeersMatch();
		}
	}

	private static void PublishHandshake()
	{
		if (!IsHost)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room == null)
			{
				return;
			}

			string value = Plugin.PluginVersion;
			object existing = null;
			if (room.CustomProperties != null && room.CustomProperties.ContainsKey(HandshakeKey))
			{
				existing = room.CustomProperties[HandshakeKey];
			}

			if (existing != null && existing.ToString() == value)
			{
				return;
			}

			Hashtable props = new Hashtable { [HandshakeKey] = value };
			room.SetCustomProperties(props);
		}
		catch
		{
		}
	}

	private static void UpdatePeersMatch()
	{
		if (IsHost)
		{
			_peersMatch = true;
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakeKey))
			{
				_peersMatch = true;
				return;
			}

			string expected = Plugin.PluginVersion;
			string actual = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			_peersMatch = string.IsNullOrEmpty(actual) || actual == expected;
			if (!_peersMatch && actual != _lastHandshakeWarn)
			{
				_lastHandshakeWarn = actual;
				Plugin.Log.LogWarning(
					(object)("MultiBoxCarry version mismatch with host. Local=" + expected + " Host=" + actual
						+ ". Install the same MultiBoxCarry.dll on all PCs."));
			}
		}
		catch
		{
			_peersMatch = true;
		}
	}
}
