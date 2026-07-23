using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Unity.IL2CPP;
using SirW_CustomHints;
using UnityEngine;

namespace SupermarketSimulatorFurnitureAligner;

internal static class FurnitureAlignerHints
{
	private static readonly List<int> ActiveHintIds = new List<int>();

	private static bool _available;

	private static bool _initialized;

	private static bool _hintsVisible;

	private static string _signature = string.Empty;

	internal static bool IsAvailable => _available;

	internal static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		try
		{
			_available = IL2CPPChainloader.Instance.Plugins.ContainsKey("SirW_CustomHints");
		}
		catch (Exception ex)
		{
			_available = false;
			FurnitureAlignerPlugin.LogSource?.LogWarning("CustomHints probe failed: " + ex.Message);
		}
	}

	internal static void Sync(bool placingMode)
	{
		Initialize();
		bool shouldShow = placingMode
			&& FurnitureAlignerRuntime.IsActive
			&& FurnitureAlignerPlugin.ShowPlacementHints != null
			&& FurnitureAlignerPlugin.ShowPlacementHints.Value
			&& _available;
		if (!shouldShow)
		{
			Clear();
			return;
		}
		string signature = BuildSignature();
		if (_hintsVisible && signature == _signature)
		{
			return;
		}
		Clear();
		ShowHints();
		_signature = signature;
		_hintsVisible = true;
	}

	internal static void NotifyStateChanged()
	{
		_signature = string.Empty;
	}

	private static void ShowHints()
	{
		try
		{
			Add(FurnitureAlignerPlugin.EdgeAlignKey.Value, "Edge Align (" + OnOff(FurnitureAlignerPlugin.EdgeAlignEnabled.Value) + ")");
			Add(FurnitureAlignerPlugin.CenterLineKey.Value, "Center Align (" + OnOff(FurnitureAlignerPlugin.CenterLineEnabled.Value) + ")");
			Add(FurnitureAlignerPlugin.GridSnapKey.Value, "Grid Snap (" + OnOff(FurnitureAlignerPlugin.GridSnapEnabled.Value) + ")");
			string outsideLabel = CoopPlacement.InMultiplayer
				? "Outside (disabled in co-op)"
				: "Outside Place (" + OnOff(FurnitureAlignerPlugin.AllowOutside.Value) + ")";
			Add(FurnitureAlignerPlugin.OutsideKey.Value, outsideLabel);
			Add(FurnitureAlignerPlugin.NudgeResetKey.Value, "Reset Nudge");
			Add(FurnitureAlignerPlugin.ToggleKey.Value, "Aligner (" + OnOff(FurnitureAlignerPlugin.Enabled.Value) + ")");
		}
		catch (Exception ex)
		{
			FurnitureAlignerPlugin.LogSource?.LogWarning("Failed to show placement hints: " + ex.Message);
			Clear();
		}
	}

	private static void Add(KeyCode key, string text)
	{
		if (key == KeyCode.None || string.IsNullOrEmpty(text))
		{
			return;
		}
		int id = CustomHints.AddHint(key, text, hintWidth: null, isPermanent: true);
		if (id >= 0)
		{
			ActiveHintIds.Add(id);
		}
	}

	private static void Clear()
	{
		if (!_hintsVisible && ActiveHintIds.Count == 0)
		{
			_signature = string.Empty;
			return;
		}
		if (_available)
		{
			try
			{
				foreach (int id in ActiveHintIds)
				{
					CustomHints.RemoveHint(id);
				}
			}
			catch (Exception ex)
			{
				FurnitureAlignerPlugin.LogSource?.LogWarning("Failed to clear placement hints: " + ex.Message);
			}
		}
		ActiveHintIds.Clear();
		_hintsVisible = false;
		_signature = string.Empty;
	}

	private static string BuildSignature()
	{
		StringBuilder sb = new StringBuilder(128);
		sb.Append(FurnitureAlignerPlugin.Enabled.Value ? '1' : '0').Append('|');
		sb.Append(FurnitureAlignerPlugin.EdgeAlignEnabled.Value ? '1' : '0').Append('|');
		sb.Append(FurnitureAlignerPlugin.CenterLineEnabled.Value ? '1' : '0').Append('|');
		sb.Append(FurnitureAlignerPlugin.GridSnapEnabled.Value ? '1' : '0').Append('|');
		sb.Append(FurnitureAlignerPlugin.AllowOutside.Value ? '1' : '0').Append('|');
		sb.Append(CoopPlacement.InMultiplayer ? '1' : '0').Append('|');
		sb.Append((int)FurnitureAlignerPlugin.EdgeAlignKey.Value).Append('|');
		sb.Append((int)FurnitureAlignerPlugin.CenterLineKey.Value).Append('|');
		sb.Append((int)FurnitureAlignerPlugin.GridSnapKey.Value).Append('|');
		sb.Append((int)FurnitureAlignerPlugin.OutsideKey.Value).Append('|');
		sb.Append((int)FurnitureAlignerPlugin.NudgeResetKey.Value).Append('|');
		sb.Append((int)FurnitureAlignerPlugin.ToggleKey.Value);
		return sb.ToString();
	}

	private static string OnOff(bool value)
	{
		return value ? "On" : "Off";
	}
}
