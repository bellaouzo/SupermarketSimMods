using System;
using Object = UnityEngine.Object;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

public sealed class SmartStockOrderTabletShortcuts : MonoBehaviour
{
	private GUIStyle _keyStyle;
	private GUIStyle _labelStyle;
	private GUIStyle _statusStyle;
	private bool _stylesReady;

	public SmartStockOrderTabletShortcuts(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		SmartStockOrderRuntime.HandleHotkeys();
	}

	private void OnGUI()
	{
		SmartStockOrderHints.Initialize();
		if (SmartStockOrderHints.IsAvailable
			|| !SmartStockOrderRuntime.IsTabletActiveForShortcuts())
		{
			return;
		}

		EnsureStyles();
		float num = 250f;
		float num2 = 128f;
		Rect val = new Rect((float)Screen.width - num - 18f, 330f, num, num2);
		GUI.Box(val, "");
		DrawShortcutRow(val.x + 12f, val.y + 12f, ((object)SmartStockOrderPlugin.TabletRefillKey.Value).ToString(), "Refill All");
		DrawShortcutRow(val.x + 12f, val.y + 50f, ((object)SmartStockOrderPlugin.TabletMinimumKey.Value).ToString(), "Zero +1");
		if (Time.unscaledTime - SmartStockOrderRuntime.LastTabletActionTime < 2f)
		{
			GUI.Label(new Rect(val.x + 14f, val.y + 88f, 220f, 28f), SmartStockOrderRuntime.LastTabletActionText, _statusStyle);
		}
	}

	private void EnsureStyles()
	{
		if (_stylesReady)
		{
			return;
		}

		_keyStyle = new GUIStyle
		{
			fontSize = 20,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleCenter
		};
		_keyStyle.normal.textColor = Color.white;
		_labelStyle = new GUIStyle
		{
			fontSize = 20,
			fontStyle = FontStyle.Bold,
			alignment = TextAnchor.MiddleLeft
		};
		_labelStyle.normal.textColor = Color.white;
		_statusStyle = new GUIStyle
		{
			fontSize = 16,
			alignment = TextAnchor.MiddleLeft
		};
		_statusStyle.normal.textColor = Color.green;
		_stylesReady = true;
	}

	private void DrawShortcutRow(float x, float y, string key, string label)
	{
		GUI.Box(new Rect(x, y, 58f, 30f), key, _keyStyle);
		GUI.Label(new Rect(x + 56f, y - 1f, 170f, 32f), label, _labelStyle);
	}
}
