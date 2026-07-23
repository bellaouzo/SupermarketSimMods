using System;
using Object = UnityEngine.Object;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

public sealed class SmartStockOrderTabletShortcuts : MonoBehaviour
{
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
		if (SmartStockOrderRuntime.IsTabletActiveForShortcuts())
		{
			float num = 250f;
			float num2 = 128f;
			Rect val = default(Rect);
			val = new Rect((float)Screen.width - num - 18f, 330f, num, num2);
			GUI.Box(val, "");
			GUIStyle val2 = new GUIStyle();
			val2.fontSize = 20;
			val2.fontStyle = (FontStyle)1;
			val2.alignment = (TextAnchor)4;
			val2.normal.textColor = Color.white;
			GUIStyle val3 = new GUIStyle();
			val3.fontSize = 20;
			val3.fontStyle = (FontStyle)1;
			val3.normal.textColor = Color.white;
			val3.alignment = (TextAnchor)3;
			DrawShortcutRow(val.x + 12f, val.y + 12f, ((object)SmartStockOrderPlugin.TabletRefillKey.Value).ToString(), "Refill All", val2, val3);
			DrawShortcutRow(val.x + 12f, val.y + 50f, ((object)SmartStockOrderPlugin.TabletMinimumKey.Value).ToString(), "Zero +1", val2, val3);
			if (Time.unscaledTime - SmartStockOrderRuntime.LastTabletActionTime < 2f)
			{
				GUIStyle val4 = new GUIStyle();
				val4.fontSize = 16;
				val4.normal.textColor = Color.green;
				val4.alignment = (TextAnchor)3;
				GUI.Label(new Rect(val.x + 14f, val.y + 88f, 220f, 28f), SmartStockOrderRuntime.LastTabletActionText, val4);
			}
		}
	}

	private static void DrawShortcutRow(float x, float y, string key, string label, GUIStyle keyStyle, GUIStyle textStyle)
	{
		GUI.Box(new Rect(x, y, 58f, 30f), key, keyStyle);
		GUI.Label(new Rect(x + 56f, y - 1f, 170f, 32f), label, textStyle);
	}
}
