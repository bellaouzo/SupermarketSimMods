using System;
using System.Globalization;
using System.Text;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeRestocker;
using ExitGames.Client.Photon;
using Il2CppInterop.Runtime;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace EmployeeTraining;

public static class TrainingNetworkSync
{
	private const string RoomPropertyKey = "etp_v1";
	private const string HandshakePropertyKey = "etp_hs";
	private const byte ChunkEventCode = 193;
	private const byte ChunkRequestEventCode = 194;
	private const int RoomPropUtf8Limit = 12000;
	private const int ChunkCharSize = 8000;
	private const string ChunkPrefix = "etp";

	private static string _lastAppliedJson = string.Empty;
	private static string _lastPublishedJson = string.Empty;
	private static string _lastHandshake = string.Empty;
	private static float _nextPublishAt;
	private static float _pendingJoinPublishAt = -1f;
	private static bool _publishDirty;
	private static bool _applyingRemote;
	private static bool _wasInRoom;
	private static bool _wasMaster;
	private static int _lastPlayerCount;
	private static float _nextGuestUiRebindAt;
	private static float _nextGuestApplyAt;
	private static int _guestUiRebindAttemptsLeft;
	private static bool _eventHooked;
	private static Il2CppSystem.Action<EventData> _eventHandler;

	private static int _incomingChunkSeq = -1;
	private static int _incomingChunkTotal;
	private static string[] _incomingChunks;
	private static int _incomingChunksReceived;
	private static float _nextChunkRequestAt;

	public static bool InMultiplayer
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

	public static bool IsHost
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

	public static bool CanGrantExp => IsHost;

	public static bool IsApplyingRemote => _applyingRemote;

	public static void Tick()
	{
		TryHookPhotonEvents();

		bool inRoom = InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			_lastPlayerCount = CurrentPlayerCount();
			_wasMaster = IsHost;
			OnJoinedRoomEdge();
		}
		else if (!inRoom && _wasInRoom)
		{
			_lastAppliedJson = string.Empty;
			_lastPublishedJson = string.Empty;
			_lastHandshake = string.Empty;
			_lastPlayerCount = 0;
			_wasMaster = false;
			_nextChunkRequestAt = 0f;
			ClearIncomingChunks();
		}
		else if (inRoom)
		{
			bool master = IsHost;
			if (master && !_wasMaster)
			{
				ScheduleJoinPublish(1f);
			}

			_wasMaster = master;
			if (master)
			{
				int playerCount = CurrentPlayerCount();
				if (playerCount > _lastPlayerCount)
				{
					ScheduleJoinPublish(2f);
				}

				_lastPlayerCount = playerCount;
				if (_pendingJoinPublishAt > 0f && Time.unscaledTime >= _pendingJoinPublishAt)
				{
					_pendingJoinPublishAt = -1f;
					if (!PublishFromHost(force: true) && ETSaveManager.Data != null)
					{
						ScheduleJoinPublish(1.5f);
					}
				}

				if (_publishDirty && Time.unscaledTime >= _nextPublishAt)
				{
					PublishFromHost(force: false);
				}
			}
		}

		_wasInRoom = inRoom;
		if (!inRoom || IsHost)
		{
			return;
		}

		if (Time.unscaledTime >= _nextGuestApplyAt)
		{
			_nextGuestApplyAt = Time.unscaledTime + 1f;
			TryApplyFromRoom();
			MaybeRequestChunkResend();
		}

		TickGuestUiRebind();
	}

	public static void ScheduleGuestUiRebind()
	{
		if (!InMultiplayer || IsHost)
		{
			return;
		}

		if (_guestUiRebindAttemptsLeft > 0)
		{
			return;
		}

		_guestUiRebindAttemptsLeft = 2;
		_nextGuestUiRebindAt = Time.unscaledTime + 1.5f;
	}

	private static void ScheduleJoinPublish(float delaySeconds)
	{
		float at = Time.unscaledTime + Mathf.Max(0.25f, delaySeconds);
		if (_pendingJoinPublishAt < 0f || at < _pendingJoinPublishAt)
		{
			_pendingJoinPublishAt = at;
		}
	}

	private static void TickGuestUiRebind()
	{
		if (_guestUiRebindAttemptsLeft <= 0 || Time.unscaledTime < _nextGuestUiRebindAt)
		{
			return;
		}

		int attempt = _guestUiRebindAttemptsLeft;
		_guestUiRebindAttemptsLeft--;
		_nextGuestUiRebindAt = Time.unscaledTime + 3f;
		try
		{
			TryApplyFromRoom();
			ETSaveManager.RebindLiveEmployees(bindWorldEmployees: false);
			if (attempt == 2)
			{
				CashierSkillManager.Instance?.BindWorldEmployees();
				RestockerSkillManager.Instance?.BindWorldEmployees();
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Guest UI rebind failed: " + ex.Message);
		}
	}

	public static void MarkPublishDirty()
	{
		_publishDirty = true;
	}

	public static void InvalidateAppliedCache()
	{
		_lastAppliedJson = string.Empty;
	}

	public static void ForceReapplyFromRoom()
	{
		InvalidateAppliedCache();
		TryApplyFromRoom();
	}

	public static void NotifyLocalSaveLoaded()
	{
		if (!InMultiplayer)
		{
			return;
		}

		if (IsHost)
		{
			ScheduleJoinPublish(0.75f);
			return;
		}

		ScheduleGuestUiRebind();
	}

	public static bool PublishFromHost(bool force = false)
	{
		if (!InMultiplayer || !IsHost || _applyingRemote || ETSaveManager.Data == null)
		{
			return false;
		}

		float now = Time.unscaledTime;
		if (!force && now < _nextPublishAt)
		{
			_publishDirty = true;
			return false;
		}

		_nextPublishAt = now + 1.25f;
		try
		{
			TrainingSaveDto dto = TrainingSaveDto.From(ETSaveManager.Data);
			string json = TrainingJson.SerializeCompact(dto);
			int utf8Bytes = Encoding.UTF8.GetByteCount(json);
			bool fitsInRoomProps = utf8Bytes <= RoomPropUtf8Limit;
			if (json == _lastPublishedJson && (fitsInRoomProps || !force))
			{
				_publishDirty = false;
				return true;
			}

			Room room = PhotonNetwork.CurrentRoom;
			if (room == null)
			{
				Plugin.LogError("Training network publish failed: CurrentRoom is null.");
				return false;
			}

			string handshake = BuildHandshake(dto);
			if (!fitsInRoomProps)
			{
				if (!PublishChunked(json, handshake, room))
				{
					Plugin.LogError(
						$"Training network publish failed entirely (chunked, {utf8Bytes} UTF8 bytes).");
					return false;
				}
			}
			else
			{
				Hashtable props = new Hashtable
				{
					[RoomPropertyKey] = json,
					[HandshakePropertyKey] = handshake
				};
				room.SetCustomProperties(props);
				Plugin.LogDebug("Published training skills to room properties.");
			}

			_lastPublishedJson = json;
			_lastAppliedJson = json;
			_lastHandshake = handshake;
			_publishDirty = false;
			return true;
		}
		catch (Exception ex)
		{
			Plugin.LogError("Training network publish failed: " + ex.Message);
			_publishDirty = true;
			return false;
		}
	}

	public static void TryApplyFromRoom()
	{
		if (!InMultiplayer || IsHost)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(RoomPropertyKey))
			{
				return;
			}

			object raw = room.CustomProperties[RoomPropertyKey];
			string json = raw != null ? raw.ToString() : null;
			if (string.IsNullOrEmpty(json) || json == _lastAppliedJson)
			{
				return;
			}

			ApplyJson(json, "room properties");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training network apply failed: " + ex.Message);
		}
	}

	private static void MaybeRequestChunkResend()
	{
		// Late-join durability: when the host's payload was too big for room props,
		// etp_v1 stays empty and data only flows via chunk events. A guest that
		// missed those events would otherwise stay blank forever.
		if (!string.IsNullOrEmpty(_lastAppliedJson) || Time.unscaledTime < _nextChunkRequestAt)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakePropertyKey))
			{
				return;
			}

			string handshake = room.CustomProperties[HandshakePropertyKey]?.ToString();
			if (string.IsNullOrEmpty(handshake))
			{
				return;
			}

			int sep = handshake.IndexOf('|');
			if (sep < 0
				|| !int.TryParse(handshake.Substring(sep + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int entries)
				|| entries <= 0)
			{
				return;
			}

			object raw = room.CustomProperties.ContainsKey(RoomPropertyKey) ? room.CustomProperties[RoomPropertyKey] : null;
			string roomJson = raw != null ? raw.ToString() : null;
			if (!string.IsNullOrEmpty(roomJson))
			{
				return;
			}

			_nextChunkRequestAt = Time.unscaledTime + 6f;
			RaiseEventOptions options = new RaiseEventOptions
			{
				Receivers = ReceiverGroup.MasterClient
			};
			if (PhotonNetwork.RaiseEvent(ChunkRequestEventCode, "etp_req", options, SendOptions.SendReliable))
			{
				Plugin.LogInfo("Requested training chunk resend from host (room prop empty, handshake has entries).");
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training chunk resend request failed: " + ex.Message);
		}
	}

	public static void OnJoinedRoomEdge()
	{
		if (IsHost)
		{
			ScheduleJoinPublish(1.5f);
		}
		else
		{
			ETSaveManager.ClearGuestEphemeralData();
			InvalidateAppliedCache();
			ScheduleGuestUiRebind();
		}
	}

	private static void TryHookPhotonEvents()
	{
		if (_eventHooked)
		{
			return;
		}

		try
		{
			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			if (client == null)
			{
				return;
			}

			_eventHandler = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<EventData>>(
				(Action<EventData>)HandleEvent);
			client.add_EventReceived(_eventHandler);
			_eventHooked = true;
			Plugin.LogInfo("Training Photon event hook ready.");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training Photon event hook failed: " + ex.Message);
		}
	}

	private static void HandleEvent(EventData photonEvent)
	{
		if (photonEvent == null)
		{
			return;
		}

		if (photonEvent.Code == ChunkRequestEventCode)
		{
			if (InMultiplayer && IsHost)
			{
				Plugin.LogInfo("Guest requested training resync; republishing.");
				ScheduleJoinPublish(0.5f);
			}

			return;
		}

		if (photonEvent.Code != ChunkEventCode || IsHost)
		{
			return;
		}

		try
		{
			string payload = photonEvent.CustomData != null ? photonEvent.CustomData.ToString() : null;
			TryAcceptChunkPayload(payload);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training chunk event apply failed: " + ex.Message);
		}
	}

	private static bool PublishChunked(string json, string handshake, Room room)
	{
		Hashtable props = new Hashtable
		{
			[HandshakePropertyKey] = handshake,
			[RoomPropertyKey] = string.Empty
		};
		room.SetCustomProperties(props);

		int total = (json.Length + ChunkCharSize - 1) / ChunkCharSize;
		if (total <= 0)
		{
			total = 1;
		}

		int seq = unchecked((int)(Time.unscaledTime * 1000f)) ^ (json.GetHashCode() & 0x7fffffff);
		RaiseEventOptions options = new RaiseEventOptions
		{
			Receivers = ReceiverGroup.Others
		};

		for (int i = 0; i < total; i++)
		{
			int start = i * ChunkCharSize;
			int len = Math.Min(ChunkCharSize, json.Length - start);
			string chunkText = json.Substring(start, len);
			string payload = string.Join("|",
				ChunkPrefix,
				seq.ToString(CultureInfo.InvariantCulture),
				total.ToString(CultureInfo.InvariantCulture),
				i.ToString(CultureInfo.InvariantCulture),
				chunkText);

			if (!PhotonNetwork.RaiseEvent(ChunkEventCode, payload, options, SendOptions.SendReliable))
			{
				Plugin.LogError($"Training chunk RaiseEvent failed at {i + 1}/{total}.");
				return false;
			}
		}

		Plugin.LogInfo($"Published training skills via {total} chunk event(s) (seq={seq}).");
		return true;
	}

	private static void TryAcceptChunkPayload(string payload)
	{
		if (string.IsNullOrEmpty(payload) || !payload.StartsWith(ChunkPrefix + "|", StringComparison.Ordinal))
		{
			return;
		}

		if (!TryParseChunk(payload, out int seq, out int total, out int index, out string chunkText))
		{
			return;
		}

		if (total <= 0 || index < 0 || index >= total)
		{
			return;
		}

		if (_incomingChunkSeq != seq || _incomingChunks == null || _incomingChunkTotal != total)
		{
			_incomingChunkSeq = seq;
			_incomingChunkTotal = total;
			_incomingChunks = new string[total];
			_incomingChunksReceived = 0;
		}

		if (_incomingChunks[index] == null)
		{
			_incomingChunks[index] = chunkText ?? string.Empty;
			_incomingChunksReceived++;
		}

		if (_incomingChunksReceived < _incomingChunkTotal)
		{
			return;
		}

		StringBuilder sb = new StringBuilder(_incomingChunkTotal * ChunkCharSize);
		for (int i = 0; i < _incomingChunkTotal; i++)
		{
			if (_incomingChunks[i] == null)
			{
				return;
			}

			sb.Append(_incomingChunks[i]);
		}

		string json = sb.ToString();
		ClearIncomingChunks();
		if (string.IsNullOrEmpty(json) || json == _lastAppliedJson)
		{
			return;
		}

		ApplyJson(json, "chunk events");
	}

	private static bool TryParseChunk(string payload, out int seq, out int total, out int index, out string chunkText)
	{
		seq = 0;
		total = 0;
		index = 0;
		chunkText = null;

		int p1 = payload.IndexOf('|');
		if (p1 < 0)
		{
			return false;
		}

		int p2 = payload.IndexOf('|', p1 + 1);
		if (p2 < 0)
		{
			return false;
		}

		int p3 = payload.IndexOf('|', p2 + 1);
		if (p3 < 0)
		{
			return false;
		}

		int p4 = payload.IndexOf('|', p3 + 1);
		if (p4 < 0)
		{
			return false;
		}

		if (!int.TryParse(payload.Substring(p1 + 1, p2 - p1 - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out seq)
			|| !int.TryParse(payload.Substring(p2 + 1, p3 - p2 - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out total)
			|| !int.TryParse(payload.Substring(p3 + 1, p4 - p3 - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
		{
			return false;
		}

		chunkText = payload.Substring(p4 + 1);
		return true;
	}

	private static void ClearIncomingChunks()
	{
		_incomingChunkSeq = -1;
		_incomingChunkTotal = 0;
		_incomingChunks = null;
		_incomingChunksReceived = 0;
	}

	private static string BuildHandshake(TrainingSaveDto dto)
	{
		int version = dto != null ? dto.Version : 0;
		int entries = CountEntries(dto);
		return version.ToString(CultureInfo.InvariantCulture) + "|" + entries.ToString(CultureInfo.InvariantCulture);
	}

	private static int CountEntries(TrainingSaveDto dto)
	{
		if (dto == null)
		{
			return 0;
		}

		return Len(dto.Cashiers) + Len(dto.Restockers) + Len(dto.CsHelpers) + Len(dto.Janitors)
			+ Len(dto.Security) + Len(dto.Bakers) + Len(dto.IceCreamHelpers);
	}

	private static int Len(SkillSaveEntry[] entries)
	{
		return entries == null ? 0 : entries.Length;
	}

	private static int CurrentPlayerCount()
	{
		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			return room != null ? room.PlayerCount : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static void ApplyJson(string json, string source)
	{
		TrainingSaveDto dto = TrainingJson.Deserialize(json);
		if (dto == null)
		{
			return;
		}

		_applyingRemote = true;
		try
		{
			ETSaveManager.ReplaceDataFromNetwork(dto.ToTrainingData());
			_lastAppliedJson = json;
			_lastHandshake = BuildHandshake(dto);
			Plugin.LogDebug(
				"Applied host training skills from " + source
				+ $". cashiers={Count(dto.Cashiers)} restockers={Count(dto.Restockers)}"
				+ SummarizeExp("cashierExp", dto.Cashiers));
		}
		finally
		{
			_applyingRemote = false;
		}

		if (_guestUiRebindAttemptsLeft <= 0)
		{
			_guestUiRebindAttemptsLeft = 1;
			_nextGuestUiRebindAt = Time.unscaledTime + 0.5f;
		}
	}

	private static int Count(SkillSaveEntry[] entries)
	{
		return entries == null ? 0 : entries.Length;
	}

	private static string SummarizeExp(string label, SkillSaveEntry[] entries)
	{
		if (entries == null || entries.Length == 0)
		{
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder();
		sb.Append(' ').Append(label).Append('=');
		for (int i = 0; i < entries.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(',');
			}

			SkillSaveEntry entry = entries[i];
			sb.Append(entry == null ? 0 : entry.Exp);
		}

		return sb.ToString();
	}
}
