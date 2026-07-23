using System;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace EmployeeTraining;

public static class TrainingNetworkSync
{
	private const string RoomPropertyKey = "etp_v1";

	private static string _lastAppliedJson = string.Empty;
	private static string _lastPublishedJson = string.Empty;
	private static float _nextPublishAt;
	private static bool _applyingRemote;
	private static bool _wasInRoom;

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
				return true;
			}
		}
	}

	public static bool CanGrantExp => IsHost;

	public static bool IsApplyingRemote => _applyingRemote;

	public static void Tick()
	{
		bool inRoom = InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			OnJoinedRoomAsHost();
		}
		else if (!inRoom && _wasInRoom)
		{
			_lastAppliedJson = string.Empty;
			_lastPublishedJson = string.Empty;
		}

		_wasInRoom = inRoom;
		if (!inRoom || IsHost)
		{
			return;
		}

		TryApplyFromRoom();
	}

	public static void PublishFromHost(bool force = false)
	{
		if (!InMultiplayer || !IsHost || _applyingRemote || ETSaveManager.Data == null)
		{
			return;
		}

		float now = Time.unscaledTime;
		if (!force && now < _nextPublishAt)
		{
			return;
		}

		_nextPublishAt = now + 0.75f;
		try
		{
			TrainingSaveDto dto = TrainingSaveDto.From(ETSaveManager.Data);
			string json = TrainingJson.Serialize(dto);
			if (!force && json == _lastPublishedJson)
			{
				return;
			}

			if (Encoding.UTF8.GetByteCount(json) > 14000)
			{
				Plugin.LogWarn("Training sync blob too large for Photon room properties; skip publish.");
				return;
			}

			Room room = PhotonNetwork.CurrentRoom;
			if (room == null)
			{
				return;
			}

			Hashtable props = new Hashtable { [RoomPropertyKey] = json };
			room.SetCustomProperties(props);
			_lastPublishedJson = json;
			_lastAppliedJson = json;
			Plugin.LogInfo("Published training skills to room properties.");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training network publish failed: " + ex.Message);
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
			string json = raw as string;
			if (string.IsNullOrEmpty(json) || json == _lastAppliedJson)
			{
				return;
			}

			ApplyJson(json);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training network apply failed: " + ex.Message);
		}
	}

	public static void OnJoinedRoomAsHost()
	{
		if (IsHost)
		{
			PublishFromHost(force: true);
		}
		else
		{
			TryApplyFromRoom();
		}
	}

	private static void ApplyJson(string json)
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
			Plugin.LogInfo("Applied host training skills from room properties.");
		}
		finally
		{
			_applyingRemote = false;
		}
	}
}
