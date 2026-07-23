using System;
using System.Globalization;
using ExitGames.Client.Photon;
using Il2CppSystem.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Il2CppObject = Il2CppSystem.Object;

namespace MultiBoxCarry;

internal static class CoopNetwork
{
	private const string AnnounceId = "mbc_hs";
	private const string RequestId = "mbc_hs_req";
	private const string OccupyId = "mbc_occupy";
	private const string VersionKey = "v";
	private const string ViewIdKey = "vid";
	private const string StateKey = "st";
	private const string ActorKey = "a";

	internal const string StateQueued = "q";
	internal const string StateHeld = "h";
	internal const string StateFree = "0";

	private static bool _subscribed;
	private static bool _wasInRoom;
	private static bool _peersMatch = true;
	private static string _lastHandshakeWarn = string.Empty;
	private static float _nextTickAt;
	private static readonly Action<Hashtable, string> NetworkHandler = OnNetworkedEvent;

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
				return NetworkUtil.Plugin.NetworkRouter.HostCheck();
			}
			catch
			{
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
	}

	internal static void Tick()
	{
		bool inRoom = CoopPlayer.InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			_lastHandshakeWarn = string.Empty;
			EnsureSubscribed();
			if (IsHost)
			{
				_peersMatch = true;
				Announce();
			}
			else
			{
				_peersMatch = false;
				RequestAnnounce();
			}
		}
		else if (!inRoom && _wasInRoom)
		{
			_peersMatch = true;
			_lastHandshakeWarn = string.Empty;
			PlayerInventoryManager.ResetAndRelease();
		}

		_wasInRoom = inRoom;
		if (!inRoom || Time.unscaledTime < _nextTickAt)
		{
			return;
		}

		_nextTickAt = Time.unscaledTime + 2.5f;
		EnsureSubscribed();
		if (IsHost)
		{
			Announce();
		}
		else if (!_peersMatch)
		{
			RequestAnnounce();
		}
	}

	internal static void EnsureSubscribed()
	{
		if (_subscribed)
		{
			return;
		}

		try
		{
			NetworkUtil.Plugin.NetworkRouter.NetworkedEvent += NetworkHandler;
			_subscribed = true;
			Plugin.Log.LogInfo((object)"[CoopNetwork] Subscribed to NetworkUtil.NetworkRouter.NetworkedEvent");
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopNetwork] Subscribe failed: " + ex.Message));
		}
	}

	internal static void BroadcastOccupy(int viewId, string state)
	{
		if (!CoopPlayer.InMultiplayer || viewId <= 0 || string.IsNullOrEmpty(state))
		{
			return;
		}

		EnsureSubscribed();
		try
		{
			Hashtable payload = CreatePayload();
			SetString(payload, ViewIdKey, viewId.ToString(CultureInfo.InvariantCulture));
			SetString(payload, StateKey, state);
			SetString(payload, ActorKey, PhotonNetwork.LocalPlayer != null
				? PhotonNetwork.LocalPlayer.ActorNumber.ToString(CultureInfo.InvariantCulture)
				: "0");
			NetworkUtil.Plugin.NetworkRouter.SendData(OccupyId, payload, OthersOptions());
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopNetwork] Occupy broadcast failed: " + ex.Message));
		}
	}

	private static void Announce()
	{
		try
		{
			Hashtable payload = CreatePayload();
			SetString(payload, VersionKey, Plugin.PluginVersion);
			NetworkUtil.Plugin.NetworkRouter.SendData(AnnounceId, payload, OthersOptions());
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopNetwork] Announce failed: " + ex.Message));
		}
	}

	private static void RequestAnnounce()
	{
		try
		{
			Hashtable payload = CreatePayload();
			SetString(payload, VersionKey, Plugin.PluginVersion);
			NetworkUtil.Plugin.NetworkRouter.SendData(RequestId, payload, MasterOptions());
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopNetwork] Request failed: " + ex.Message));
		}
	}

	private static void OnNetworkedEvent(Hashtable payload, string messageId)
	{
		if (string.IsNullOrEmpty(messageId) || payload == null)
		{
			return;
		}

		try
		{
			if (messageId == RequestId)
			{
				if (IsHost)
				{
					Announce();
				}

				return;
			}

			if (messageId == AnnounceId)
			{
				HandleAnnounce(payload);
				return;
			}

			if (messageId == OccupyId)
			{
				HandleOccupy(payload);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogWarning((object)("[CoopNetwork] Event handler failed: " + ex.Message));
		}
	}

	private static void HandleAnnounce(Hashtable payload)
	{
		if (!TryGetString(payload, VersionKey, out string remoteVersion))
		{
			return;
		}

		bool match = string.Equals(remoteVersion, Plugin.PluginVersion, StringComparison.Ordinal);
		_peersMatch = match;
		if (match)
		{
			_lastHandshakeWarn = string.Empty;
			return;
		}

		string warn = "[CoopNetwork] MultiBoxCarry version mismatch. Local=" + Plugin.PluginVersion + " Remote=" + remoteVersion;
		if (_lastHandshakeWarn != warn)
		{
			_lastHandshakeWarn = warn;
			Plugin.Log.LogWarning((object)warn);
		}
	}

	private static void HandleOccupy(Hashtable payload)
	{
		if (!TryGetString(payload, ViewIdKey, out string viewText)
			|| !int.TryParse(viewText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int viewId)
			|| viewId <= 0)
		{
			return;
		}

		if (!TryGetString(payload, StateKey, out string state))
		{
			return;
		}

		if (TryGetString(payload, ActorKey, out string actorText)
			&& int.TryParse(actorText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int actor)
			&& PhotonNetwork.LocalPlayer != null
			&& actor == PhotonNetwork.LocalPlayer.ActorNumber)
		{
			return;
		}

		NetworkBoxSync.ApplyRemoteOccupy(viewId, state != StateFree);
	}

	private static RaiseEventOptions OthersOptions()
	{
		return new RaiseEventOptions
		{
			Receivers = ReceiverGroup.Others
		};
	}

	private static RaiseEventOptions MasterOptions()
	{
		return new RaiseEventOptions
		{
			Receivers = ReceiverGroup.MasterClient
		};
	}

	private static Hashtable CreatePayload()
	{
		return new Hashtable();
	}

	private static void SetString(Hashtable payload, string key, string value)
	{
		Il2CppObject boxedKey = key;
		Il2CppObject boxedValue = value ?? string.Empty;
		payload[boxedKey] = boxedValue;
	}

	private static bool TryGetString(Hashtable payload, string key, out string value)
	{
		value = string.Empty;
		Il2CppObject boxedKey = key;
		if (!((Dictionary<Il2CppObject, Il2CppObject>)(object)payload).TryGetValue(boxedKey, out Il2CppObject boxed) || boxed == null)
		{
			return false;
		}

		value = boxed.ToString() ?? string.Empty;
		return !string.IsNullOrEmpty(value);
	}
}
