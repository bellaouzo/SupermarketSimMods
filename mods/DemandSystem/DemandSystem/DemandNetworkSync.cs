using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace DemandSystem;

internal static class DemandNetworkSync
{
	private const string StateKey = "ds_v1";
	private const string HandshakeKey = "ds_hs";

	private static bool _wasInRoom;
	private static string _lastAppliedState = string.Empty;
	private static string _lastPublishedState = string.Empty;
	private static string _lastHandshakeWarn = string.Empty;
	private static bool _peersMatch = true;

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

	internal static bool IsHost
	{
		get
		{
			if (!InMultiplayer)
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

	internal static bool PeersMatch => !InMultiplayer || _peersMatch;

	internal static bool ShouldGenerateLocally => !InMultiplayer || IsHost;

	internal static void Tick()
	{
		bool inRoom = InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			if (IsHost)
			{
				PublishHandshake();
				PublishState(force: true);
			}
			else
			{
				TryApplyState();
			}
		}
		else if (!inRoom && _wasInRoom)
		{
			_lastAppliedState = string.Empty;
			_lastPublishedState = string.Empty;
			_peersMatch = true;
		}

		_wasInRoom = inRoom;
		if (!inRoom)
		{
			return;
		}

		UpdatePeersMatch();
		if (IsHost)
		{
			PublishHandshake();
			PublishState(force: false);
		}
		else
		{
			TryApplyState();
		}
	}

	internal static void PublishState(bool force)
	{
		if (!InMultiplayer || !IsHost)
		{
			return;
		}

		string state = DemandState.BuildNetworkState();
		if (!force && state == _lastPublishedState)
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

			Hashtable props = new Hashtable { [StateKey] = state };
			room.SetCustomProperties(props);
			_lastPublishedState = state;
			_lastAppliedState = state;
		}
		catch (Exception ex)
		{
			DemandPlugin.LogSource.LogWarning((object)("Demand publish failed: " + ex.Message));
		}
	}

	internal static void NotifyHostGenerated()
	{
		PublishState(force: true);
	}

	internal static string CfgHash()
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 31 + DemandPlugin.Enabled.Value.GetHashCode();
			hash = hash * 31 + DemandPlugin.EventChancePercent.Value.GetHashCode();
			hash = hash * 31 + DemandPlugin.TwoProductsChancePercent.Value.GetHashCode();
			hash = hash * 31 + DemandPlugin.ThreeProductsChancePercent.Value.GetHashCode();
			hash = hash * 31 + DemandPlugin.CustomerDemandChancePercent.Value;
			hash = hash * 31 + DemandPlugin.ExtraItemsMin.Value;
			hash = hash * 31 + DemandPlugin.ExtraItemsMax.Value;
			return hash.ToString("X8");
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

			string value = DemandPlugin.PluginVersion + "|" + CfgHash();
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

			string expected = DemandPlugin.PluginVersion + "|" + CfgHash();
			string actual = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			_peersMatch = string.IsNullOrEmpty(actual) || actual == expected;
			if (!_peersMatch && actual != _lastHandshakeWarn)
			{
				_lastHandshakeWarn = actual;
				DemandPlugin.LogSource.LogWarning(
					(object)("Demand cfg/version mismatch with host. Local=" + expected + " Host=" + actual
						+ ". Match Demand.dll + Demand.cfg on all PCs."));
			}
		}
		catch
		{
			_peersMatch = true;
		}
	}

	private static void TryApplyState()
	{
		if (!InMultiplayer || IsHost)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(StateKey))
			{
				return;
			}

			string state = room.CustomProperties[StateKey]?.ToString() ?? string.Empty;
			if (string.IsNullOrEmpty(state) || state == _lastAppliedState)
			{
				return;
			}

			if (DemandState.ApplyNetworkState(state))
			{
				_lastAppliedState = state;
			}
		}
		catch (Exception ex)
		{
			DemandPlugin.LogSource.LogWarning((object)("Demand apply failed: " + ex.Message));
		}
	}
}
