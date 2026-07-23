using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Unity.IL2CPP;
using SirW_CustomHints;
using UnityEngine;

namespace SupermarketSimulatorShelfProductSwapper;

internal static class ShelfProductSwapperHints
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
			ShelfProductSwapperPlugin.LogSource?.LogWarning("CustomHints probe failed: " + ex.Message);
		}
	}

	internal static void Sync(bool enabled)
	{
		Initialize();
		bool aimingOrSelected = ShelfProductSwapperRuntime.HasSelection || ShelfProductSwapperRuntime.HasHoverTarget;
		bool shouldShow = enabled
			&& aimingOrSelected
			&& ShelfProductSwapperPlugin.ShowHints != null
			&& ShelfProductSwapperPlugin.ShowHints.Value
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
			Add(ShelfProductSwapperPlugin.SwapKey.Value, ShelfProductSwapperRuntime.HasSelection ? "Swap Target" : "Select Slot");
			Add(ShelfProductSwapperPlugin.ToggleIncompatibleKey.Value, "Cross-Type (" + OnOff(ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value) + ")");
			Add(ShelfProductSwapperPlugin.ToggleModKey.Value, "Swapper (" + OnOff(true) + ")");
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource?.LogWarning("Failed to show swap hints: " + ex.Message);
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
				ShelfProductSwapperPlugin.LogSource?.LogWarning("Failed to clear swap hints: " + ex.Message);
			}
		}
		ActiveHintIds.Clear();
		_hintsVisible = false;
		_signature = string.Empty;
	}

	private static string BuildSignature()
	{
		StringBuilder sb = new StringBuilder(64);
		sb.Append(ShelfProductSwapperRuntime.HasSelection ? '1' : '0').Append('|');
		sb.Append(ShelfProductSwapperRuntime.HasHoverTarget ? '1' : '0').Append('|');
		sb.Append(ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value ? '1' : '0').Append('|');
		sb.Append((int)ShelfProductSwapperPlugin.SwapKey.Value).Append('|');
		sb.Append((int)ShelfProductSwapperPlugin.ToggleIncompatibleKey.Value).Append('|');
		sb.Append((int)ShelfProductSwapperPlugin.ToggleModKey.Value);
		return sb.ToString();
	}

	private static string OnOff(bool value)
	{
		return value ? "On" : "Off";
	}
}
