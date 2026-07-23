using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace SupermarketSimulatorFurnitureAligner;

internal static class CoopHandshake
{
	private const string HandshakeKey = "fa_hs";

	private static bool _wasInRoom;
	private static string _lastHandshakeWarn = string.Empty;

	internal static bool IsHost
	{
		get
		{
			if (!CoopPlacement.InMultiplayer)
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
		bool inRoom = CoopPlacement.InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			if (IsHost)
			{
				PublishHandshake();
			}
			else
			{
				CheckMismatch();
			}
		}
		else if (!inRoom && _wasInRoom)
		{
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
		}
		else
		{
			CheckMismatch();
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

			string value = FurnitureAlignerPlugin.PluginVersion;
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

	private static void CheckMismatch()
	{
		if (IsHost)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakeKey))
			{
				return;
			}

			string expected = FurnitureAlignerPlugin.PluginVersion;
			string actual = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			if (string.IsNullOrEmpty(actual) || actual == expected || actual == _lastHandshakeWarn)
			{
				return;
			}

			_lastHandshakeWarn = actual;
			FurnitureAlignerPlugin.LogSource.LogWarning(
				(object)("Furniture Aligner version mismatch with host. Local=" + expected + " Host=" + actual
					+ ". Install the same FurnitureAligner.dll on all PCs."));
		}
		catch
		{
		}
	}
}
