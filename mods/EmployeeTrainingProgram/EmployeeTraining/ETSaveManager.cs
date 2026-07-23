using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;
using HarmonyLib;
using MyBox;
using UnityEngine;

namespace EmployeeTraining;

public static class ETSaveManager
{
	private static readonly string SAVE_DIR = Path.Combine(Application.persistentDataPath, "EmployeeTraining");

	public static bool IsReadyToSave = false;

	public static Action SaveDataLoadedEvent;

	public static TrainingData Data { get; private set; } = new TrainingData();

	private static float _nextSaveAllowedAt;

	private static bool _loggedGuestSkipSave;

	public static void CreateSaveDirectory()
	{
		if (!Directory.Exists(SAVE_DIR))
		{
			Plugin.LogInfo("Creating savedata directory: " + SAVE_DIR);
			Directory.CreateDirectory(SAVE_DIR);
		}
	}

	private static string GetSaveFilePath(string gameFilePath)
	{
		string fileName = Path.GetFileNameWithoutExtension(gameFilePath);
		if (string.IsNullOrEmpty(fileName))
		{
			fileName = "unknown";
		}
		return Path.Combine(SAVE_DIR, "EmployeeTraining-" + fileName + ".json");
	}

	public static void Load(string gameFilePath, int tries = -1)
	{
		if (string.IsNullOrEmpty(gameFilePath))
		{
			Plugin.LogInfo("Training Load skipped: empty game save path");
			IsReadyToSave = true;
			TrainingNetworkSync.NotifyLocalSaveLoaded();
			return;
		}
		try
		{
			if (TrainingNetworkSync.InMultiplayer && !TrainingNetworkSync.IsHost)
			{
				Plugin.LogInfo("Guest in multiplayer: preferring host training room sync over local disk load.");
				TrainingNetworkSync.ScheduleGuestUiRebind();
				return;
			}

			CreateSaveDirectory();
			string saveFilePath = GetSaveFilePath(gameFilePath);
			Plugin.LogInfo("Loading training data from " + saveFilePath);
			if (File.Exists(saveFilePath))
			{
				string json = File.ReadAllText(saveFilePath, Encoding.UTF8);
				TrainingSaveDto dto = TrainingJson.Deserialize(json);
				if (dto != null && TotalEntries(dto) > 0)
				{
					Data = dto.ToTrainingData();
					Plugin.LogInfo(
						$"Training data loaded: cashiers={Data.CashierSkills.Count}, restockers={Data.RestockerSkills.Count}, janitors={Data.JanitorSkills.Count}, bakers={Data.BakerSkills.Count}, janitorExp={SumExp(Data.JanitorSkills)}");
					NotifySaveDataLoaded();
					return;
				}
				if (TotalEntries(Data) > 0)
				{
					Plugin.LogWarn("Disk training save was empty; keeping in-memory training data.");
					SyncAllSkills();
					return;
				}
			}
			if (TotalEntries(Data) > 0)
			{
				Plugin.LogInfo("No disk training save; keeping in-memory training data.");
				SyncAllSkills();
				return;
			}
			Plugin.LogInfo("Training data NOT FOUND (starting fresh for this slot)");
			Data = new TrainingData();
		}
		catch (Exception ex)
		{
			Plugin.LogError("Failed to load training data!");
			Plugin.LogError(ex);
		}
		finally
		{
			IsReadyToSave = true;
			TrainingNetworkSync.NotifyLocalSaveLoaded();
		}
	}

	public static void Save(string gameFilePath)
	{
		if (string.IsNullOrEmpty(gameFilePath))
		{
			Plugin.LogWarn("Training Save skipped: empty game save path");
			return;
		}
		if (TrainingNetworkSync.InMultiplayer && !TrainingNetworkSync.IsHost)
		{
			if (!_loggedGuestSkipSave)
			{
				Plugin.LogDebug("Guest skipping training disk write in multiplayer.");
				_loggedGuestSkipSave = true;
			}
			return;
		}
		IsReadyToSave = true;
		try
		{
			CreateSaveDirectory();
			string saveFilePath = GetSaveFilePath(gameFilePath);
			TrainingSaveDto dto = TrainingSaveDto.From(Data);
			int incoming = TotalEntries(dto);
			if (incoming == 0 && File.Exists(saveFilePath))
			{
				TrainingSaveDto existing = TrainingJson.Deserialize(File.ReadAllText(saveFilePath, Encoding.UTF8));
				if (existing != null && TotalEntries(existing) > 0)
				{
					Plugin.LogWarn("Refusing to overwrite training save with empty data.");
					return;
				}
			}
			string json = TrainingJson.Serialize(dto);
			string tempPath = saveFilePath + ".tmp";
			File.WriteAllText(tempPath, json, Encoding.UTF8);
			if (File.Exists(saveFilePath))
			{
				File.Delete(saveFilePath);
			}
			File.Move(tempPath, saveFilePath);
			Plugin.LogInfo(
				$"Saved training data to {saveFilePath} (cashiers={Count(dto.Cashiers)}, restockers={Count(dto.Restockers)}, janitors={Count(dto.Janitors)}, janitorExp={SumExp(dto.Janitors)}, bakers={Count(dto.Bakers)})");
			DeleteOldestSaveWhenMaxed();
		}
		catch (Exception ex)
		{
			Plugin.LogError("Failed to save training data!");
			Plugin.LogError(ex);
		}
	}

	public static void SaveCurrent(bool force = false)
	{
		try
		{
			if (TrainingNetworkSync.IsApplyingRemote)
			{
				return;
			}

			if (TrainingNetworkSync.InMultiplayer && !TrainingNetworkSync.IsHost)
			{
				if (!_loggedGuestSkipSave)
				{
					Plugin.LogDebug("Guest skipping training disk write in multiplayer.");
					_loggedGuestSkipSave = true;
				}
				return;
			}

			float now = Time.unscaledTime;
			if (!force && now < _nextSaveAllowedAt)
			{
				return;
			}
			_nextSaveAllowedAt = now + 0.75f;
			SaveManager saveManager = Singleton<SaveManager>.Instance;
			string path = saveManager != null ? saveManager.CurrentSaveFilePath : null;
			if (string.IsNullOrEmpty(path))
			{
				path = "slot_0.es3";
			}
			Save(path);
			TrainingNetworkSync.PublishFromHost(force);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("SaveCurrent failed: " + ex.Message);
		}
	}

	public static void ReplaceDataFromNetwork(TrainingData data)
	{
		if (data == null)
		{
			return;
		}

		Data = data;
		SyncAllSkills();
		RebindLiveEmployees();
		TrainingNetworkSync.ScheduleGuestUiRebind();
	}

	public static void RebindLiveEmployees()
	{
		try
		{
			CashierSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Cashier rebind after network apply failed: " + ex.Message);
		}

		try
		{
			RestockerSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Restocker rebind after network apply failed: " + ex.Message);
		}

		try
		{
			BakerSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Baker rebind after network apply failed: " + ex.Message);
		}

		try
		{
			IceCreamHelperSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Ice cream rebind after network apply failed: " + ex.Message);
		}

		try
		{
			JanitorSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Janitor rebind after network apply failed: " + ex.Message);
		}

		try
		{
			SecuritySkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Security rebind after network apply failed: " + ex.Message);
		}

		try
		{
			CsHelperSkillManager.Instance?.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("CsHelper rebind after network apply failed: " + ex.Message);
		}
	}

	public static void Clear()
	{
		Plugin.LogDebug("Clearing training data");
		Data = new TrainingData();
	}

	public static void SyncAllSkills()
	{
		if (Data == null)
		{
			return;
		}

		foreach (var entry in Data.CashierSkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.RestockerSkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.CsHelperSkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.JanitorSkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.SecuritySkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.BakerSkills)
		{
			entry?.SyncFromSave();
		}
		foreach (var entry in Data.IceCreamHelperSkills)
		{
			entry?.SyncFromSave();
		}
	}

	private static void NotifySaveDataLoaded()
	{
		try
		{
			SaveDataLoadedEvent?.Invoke();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("SaveDataLoadedEvent failed: " + ex.Message);
		}
		SyncAllSkills();
	}

	private static int Count(SkillSaveEntry[] entries)
	{
		return entries == null ? 0 : entries.Length;
	}

	private static int TotalEntries(TrainingSaveDto dto)
	{
		if (dto == null)
		{
			return 0;
		}
		return Count(dto.Cashiers) + Count(dto.Restockers) + Count(dto.CsHelpers) + Count(dto.Janitors)
			+ Count(dto.Security) + Count(dto.Bakers) + Count(dto.IceCreamHelpers);
	}

	private static int TotalEntries(TrainingData data)
	{
		if (data == null)
		{
			return 0;
		}
		return data.CashierSkills.Count + data.RestockerSkills.Count + data.CsHelperSkills.Count
			+ data.JanitorSkills.Count + data.SecuritySkills.Count + data.BakerSkills.Count
			+ data.IceCreamHelperSkills.Count;
	}

	private static int SumExp(SkillSaveEntry[] entries)
	{
		if (entries == null)
		{
			return 0;
		}
		int sum = 0;
		foreach (SkillSaveEntry e in entries)
		{
			if (e != null)
			{
				sum += e.Exp;
			}
		}
		return sum;
	}

	private static int SumExp(System.Collections.Generic.List<EmployeeJanitor.JanitorSkillData> entries)
	{
		if (entries == null)
		{
			return 0;
		}
		int sum = 0;
		foreach (EmployeeJanitor.JanitorSkillData e in entries)
		{
			if (e != null)
			{
				sum += e.Exp;
			}
		}
		return sum;
	}

	private static void DeleteOldestSaveWhenMaxed()
	{
		try
		{
			SaveManager saveManager = Singleton<SaveManager>.Instance;
			int max = 20;
			if (saveManager != null)
			{
				int value = Traverse.Create((object)saveManager).Field("m_MaxBackupCount").GetValue<int>();
				if (value > 0)
				{
					max = value;
				}
			}
			string[] files = Directory.GetFiles(SAVE_DIR, "EmployeeTraining-*.json");
			if (files.Length <= max)
			{
				return;
			}
			files = files.OrderByDescending((string f) => new FileInfo(f).LastWriteTime).ToArray();
			for (int i = max; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Training save cleanup failed: " + ex.Message);
		}
	}
}

internal static class TrainingJson
{
	public static string Serialize(TrainingSaveDto dto)
	{
		StringBuilder sb = new StringBuilder(512);
		sb.AppendLine("{");
		sb.AppendLine("  \"Version\": " + dto.Version.ToString(CultureInfo.InvariantCulture) + ",");
		WriteArray(sb, "Cashiers", dto.Cashiers, false);
		WriteArray(sb, "Restockers", dto.Restockers, false);
		WriteArray(sb, "CsHelpers", dto.CsHelpers, false);
		WriteArray(sb, "Janitors", dto.Janitors, false);
		WriteArray(sb, "Security", dto.Security, false);
		WriteArray(sb, "Bakers", dto.Bakers, false);
		WriteArray(sb, "IceCreamHelpers", dto.IceCreamHelpers, true);
		sb.AppendLine("}");
		return sb.ToString();
	}

	public static TrainingSaveDto Deserialize(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}
		TrainingSaveDto dto = new TrainingSaveDto();
		Match version = Regex.Match(json, "\"Version\"\\s*:\\s*(-?\\d+)");
		if (version.Success)
		{
			dto.Version = int.Parse(version.Groups[1].Value, CultureInfo.InvariantCulture);
		}
		dto.Cashiers = ReadArray(json, "Cashiers");
		dto.Restockers = ReadArray(json, "Restockers");
		dto.CsHelpers = ReadArray(json, "CsHelpers");
		dto.Janitors = ReadArray(json, "Janitors");
		dto.Security = ReadArray(json, "Security");
		dto.Bakers = ReadArray(json, "Bakers");
		dto.IceCreamHelpers = ReadArray(json, "IceCreamHelpers");
		return dto;
	}

	private static void WriteArray(StringBuilder sb, string name, SkillSaveEntry[] entries, bool last)
	{
		sb.Append("  \"").Append(name).Append("\": [");
		if (entries != null && entries.Length > 0)
		{
			sb.AppendLine();
			for (int i = 0; i < entries.Length; i++)
			{
				SkillSaveEntry e = entries[i] ?? new SkillSaveEntry();
				sb.Append("    {\"Id\": ").Append(e.Id.ToString(CultureInfo.InvariantCulture));
				sb.Append(", \"Exp\": ").Append(e.Exp.ToString(CultureInfo.InvariantCulture));
				sb.Append(", \"Grade\": ").Append(e.Grade.ToString(CultureInfo.InvariantCulture));
				sb.Append(", \"IsGaugeDisplayed\": ").Append(e.IsGaugeDisplayed ? "true" : "false");
				sb.Append("}");
				sb.AppendLine(i < entries.Length - 1 ? "," : "");
			}
			sb.Append("  ]");
		}
		else
		{
			sb.Append("]");
		}
		sb.AppendLine(last ? "" : ",");
	}

	private static SkillSaveEntry[] ReadArray(string json, string name)
	{
		Match block = Regex.Match(json, "\"" + name + "\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
		if (!block.Success || string.IsNullOrWhiteSpace(block.Groups[1].Value))
		{
			return Array.Empty<SkillSaveEntry>();
		}
		MatchCollection objects = Regex.Matches(block.Groups[1].Value, "\\{([^}]*)\\}");
		SkillSaveEntry[] result = new SkillSaveEntry[objects.Count];
		for (int i = 0; i < objects.Count; i++)
		{
			string body = objects[i].Groups[1].Value;
			result[i] = new SkillSaveEntry
			{
				Id = ReadInt(body, "Id"),
				Exp = ReadInt(body, "Exp"),
				Grade = ReadInt(body, "Grade"),
				IsGaugeDisplayed = ReadBool(body, "IsGaugeDisplayed", true)
			};
		}
		return result;
	}

	private static int ReadInt(string body, string key)
	{
		Match m = Regex.Match(body, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
		return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
	}

	private static bool ReadBool(string body, string key, bool fallback)
	{
		Match m = Regex.Match(body, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
		if (!m.Success)
		{
			return fallback;
		}
		return string.Equals(m.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
	}
}
