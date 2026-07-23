using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

internal static class CoopHandshake
{
	private const string HandshakeKey = "sso_hs";

	private static bool _wasInRoom;
	private static bool _peersMatch = true;
	private static string _lastHandshakeWarn = string.Empty;
	private static bool _bulkGateWarned;
	private static float _nextTickAt;

	internal static bool PeersMatch => !NetworkCartUtil.InMultiplayer || _peersMatch;

	internal static bool IsHost
	{
		get
		{
			if (!NetworkCartUtil.InMultiplayer)
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
		float now = Time.unscaledTime;
		if (now < _nextTickAt)
		{
			return;
		}

		_nextTickAt = now + 8f;
		bool inRoom = NetworkCartUtil.InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			_bulkGateWarned = false;
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
			_peersMatch = true;
			_lastHandshakeWarn = string.Empty;
			_bulkGateWarned = false;
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

	internal static bool WarnBulkGateOnce()
	{
		if (_bulkGateWarned)
		{
			return false;
		}

		_bulkGateWarned = true;
		SmartStockOrderPlugin.LogSource.LogWarning(
			(object)"Smart Stock Order bulk-add blocked: co-op peers have mismatched version/cfg. Match SmartStockOrder.dll + MaxBoxesPerRun/RemoveCartLimit on all PCs.");
		return true;
	}

	private static string BuildHandshakeValue()
	{
		int maxBoxes = SmartStockOrderPlugin.MaxBoxesPerRun != null ? SmartStockOrderPlugin.MaxBoxesPerRun.Value : 500;
		bool removeLimit = SmartStockOrderPlugin.RemoveCartLimit != null && SmartStockOrderPlugin.RemoveCartLimit.Value;
		bool includeRack = SmartStockOrderPlugin.IncludeRackShortage == null || SmartStockOrderPlugin.IncludeRackShortage.Value;
		bool includeDisplay = SmartStockOrderPlugin.IncludeDisplayShelfShortage != null && SmartStockOrderPlugin.IncludeDisplayShelfShortage.Value;
		bool boxOnly = SmartStockOrderPlugin.MinimumUsesBoxStockOnly == null || SmartStockOrderPlugin.MinimumUsesBoxStockOnly.Value;
		int cartOverride = SmartStockOrderPlugin.CartLimitOverride != null ? SmartStockOrderPlugin.CartLimitOverride.Value : 9999;
		return SmartStockOrderPlugin.PluginVersion
			+ "|" + maxBoxes
			+ "|" + (removeLimit ? "1" : "0")
			+ "|" + (includeRack ? "1" : "0")
			+ "|" + (includeDisplay ? "1" : "0")
			+ "|" + (boxOnly ? "1" : "0")
			+ "|" + cartOverride;
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

			string value = BuildHandshakeValue();
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
			int playerCount = room != null ? room.PlayerCount : 0;
			bool hasOthers = playerCount > 1;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakeKey))
			{
				_peersMatch = !hasOthers;
				if (!_peersMatch && _lastHandshakeWarn != "missing")
				{
					_lastHandshakeWarn = "missing";
					SmartStockOrderPlugin.LogSource.LogWarning(
						(object)("Smart Stock Order handshake missing (expected sso_hs). Match SmartStockOrder.dll on all PCs. Bulk-add blocked."));
				}

				return;
			}

			string expected = BuildHandshakeValue();
			string actual = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			_peersMatch = !string.IsNullOrEmpty(actual) && actual == expected;
			if (!_peersMatch && actual != _lastHandshakeWarn)
			{
				_lastHandshakeWarn = actual;
				SmartStockOrderPlugin.LogSource.LogWarning(
					(object)("Smart Stock Order cfg/version mismatch with host. Local=" + expected + " Host=" + actual
						+ ". Match SmartStockOrder.dll + MaxBoxesPerRun/RemoveCartLimit on all PCs."));
			}
			else if (_peersMatch)
			{
				_lastHandshakeWarn = string.Empty;
			}
		}
		catch
		{
			_peersMatch = false;
		}
	}
}
