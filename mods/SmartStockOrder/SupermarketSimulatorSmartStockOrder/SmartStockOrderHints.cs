using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Unity.IL2CPP;
using SirW_CustomHints;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

internal static class SmartStockOrderHints
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
			SmartStockOrderPlugin.LogSource?.LogWarning("CustomHints probe failed: " + ex.Message);
		}
	}

	internal static void Sync(bool show)
	{
		Initialize();
		bool shouldShow = show
			&& SmartStockOrderPlugin.ShowHints != null
			&& SmartStockOrderPlugin.ShowHints.Value
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

	private static void ShowHints()
	{
		try
		{
			Add(SmartStockOrderPlugin.TabletRefillKey.Value, "Refill All");
			Add(SmartStockOrderPlugin.TabletMinimumKey.Value, "Zero +1");
		}
		catch (Exception ex)
		{
			SmartStockOrderPlugin.LogSource?.LogWarning("Failed to show stock order hints: " + ex.Message);
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
				SmartStockOrderPlugin.LogSource?.LogWarning("Failed to clear stock order hints: " + ex.Message);
			}
		}
		ActiveHintIds.Clear();
		_hintsVisible = false;
		_signature = string.Empty;
	}

	private static string BuildSignature()
	{
		StringBuilder sb = new StringBuilder(32);
		sb.Append((int)SmartStockOrderPlugin.TabletRefillKey.Value).Append('|');
		sb.Append((int)SmartStockOrderPlugin.TabletMinimumKey.Value);
		return sb.ToString();
	}
}
