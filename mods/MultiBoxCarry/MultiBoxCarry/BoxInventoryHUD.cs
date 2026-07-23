using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace MultiBoxCarry;

internal sealed class BoxInventoryHUD : MonoBehaviour
{
	private const float ScrollCooldown = 0.12f;
	private const float HudRefreshInterval = 0.1f;

	private bool _created;
	private GameObject _root;
	private TextMeshProUGUI _text;
	private PlayerObjectHolder _holder;
	private PlayerInteraction _player;
	private int _selectedIndex;
	private float _nextScrollTime;
	private float _nextHudRefreshTime;
	private string _lastText = string.Empty;
	private readonly StringBuilder _sb = new StringBuilder(256);

	public BoxInventoryHUD(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		if (!_created)
		{
			TryCreateHud();
			if (!_created)
			{
				return;
			}
		}

		RefreshPlayerRefs();

		if ((Object)(object)_player != (Object)null && !CoopPlayer.IsLocal(_player))
		{
			if ((Object)(object)_root != (Object)null && _root.activeSelf)
			{
				_root.SetActive(false);
			}

			return;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(_player);
		int queued = inventory?.Count ?? 0;
		bool holdingSomething = queued > 0
			|| ((Object)(object)_holder != (Object)null && (Object)(object)_holder.CurrentObject != (Object)null);

		if (!holdingSomething)
		{
			if ((Object)(object)_root != (Object)null && _root.activeSelf)
			{
				_root.SetActive(false);
				_lastText = string.Empty;
			}

			_selectedIndex = 0;
			return;
		}

		HandleInput(inventory);
		if (Time.unscaledTime >= _nextHudRefreshTime)
		{
			_nextHudRefreshTime = Time.unscaledTime + HudRefreshInterval;
			UpdateHudText(inventory);
		}
	}

	private void RefreshPlayerRefs()
	{
		_player = CoopPlayer.GetLocalPlayerInteraction();
		_holder = (Object)(object)_player == (Object)null
			? null
			: ((Component)_player).GetComponent<PlayerObjectHolder>();
	}

	private void HandleInput(BoxInventory inventory)
	{
		if ((Object)(object)_player == (Object)null)
		{
			return;
		}

		int queued = inventory?.Count ?? 0;
		if (queued <= 0)
		{
			_selectedIndex = 0;
			return;
		}

		IQueuableBox held = BoxUtility.GetHeldQueueBox(_holder);
		int total = (held != null ? 1 : 0) + queued;
		if (total <= 1)
		{
			_selectedIndex = 0;
			return;
		}

		if (_selectedIndex < 0)
		{
			_selectedIndex = 0;
		}

		if (_selectedIndex >= total)
		{
			_selectedIndex = total - 1;
		}

		bool selectMode = PluginConfig.SelectThenConfirm.Value;
		int delta = 0;

		if (PluginConfig.CycleWithScroll.Value && Time.unscaledTime >= _nextScrollTime)
		{
			Mouse mouse = Mouse.current;
			if (mouse != null)
			{
				float scroll = mouse.scroll.ReadValue().y;
				if (Mathf.Abs(scroll) > 0.01f)
				{
					delta = scroll > 0f ? -1 : 1;
					if (PluginConfig.InvertScroll.Value)
					{
						delta = -delta;
					}

					_nextScrollTime = Time.unscaledTime + ScrollCooldown;
				}
			}
		}

		Keyboard keyboard = Keyboard.current;
		if (keyboard != null)
		{
			Key nextKey = PluginConfig.CycleNextKey.Value;
			Key prevKey = PluginConfig.CyclePrevKey.Value;
			if (nextKey != Key.None && WasPressed(keyboard, nextKey))
			{
				delta = 1;
			}
			else if (prevKey != Key.None && WasPressed(keyboard, prevKey))
			{
				delta = -1;
			}
		}

		if (delta == 0)
		{
			if (selectMode && keyboard != null)
			{
				Key confirmKey = PluginConfig.ConfirmSwitchKey.Value;
				if (confirmKey != Key.None && WasPressed(keyboard, confirmKey) && _selectedIndex > 0)
				{
					BoxInventoryController.TrySwitchToQueueIndex(_player, _selectedIndex - 1);
					_selectedIndex = 0;
					_nextHudRefreshTime = 0f;
				}
			}

			return;
		}

		if (selectMode)
		{
			_selectedIndex = Wrap(_selectedIndex + delta, total);
			_nextHudRefreshTime = 0f;
			return;
		}

		BoxInventoryController.TryCycleBoxes(_player, delta);
		_selectedIndex = 0;
		_nextHudRefreshTime = 0f;
	}

	private static bool WasPressed(Keyboard keyboard, Key key)
	{
		try
		{
			KeyControl control = keyboard[key];
			return control != null && control.wasPressedThisFrame;
		}
		catch
		{
			return false;
		}
	}

	private static int Wrap(int value, int count)
	{
		if (count <= 0)
		{
			return 0;
		}

		int result = value % count;
		if (result < 0)
		{
			result += count;
		}

		return result;
	}

	private void TryCreateHud()
	{
		GameObject uiRoot = GameObject.Find("---UI---");
		if ((Object)(object)uiRoot == (Object)null)
		{
			return;
		}

		Transform canvas = uiRoot.transform.Find("Ingame Canvas");
		if ((Object)(object)canvas == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Ingame Canvas not found");
			return;
		}

		Transform timeTransform = canvas.Find("Time");
		if ((Object)(object)timeTransform == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Time not found");
			return;
		}

		TextMeshProUGUI sourceTmp = ((Component)timeTransform).GetComponent<TextMeshProUGUI>()
			?? ((Component)timeTransform).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)sourceTmp == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Could not find source TMP on Time");
			return;
		}

		RefreshPlayerRefs();
		_root = new GameObject("MultiBoxCarry_ListHUD");
		_root.transform.SetParent(canvas, false);

		RectTransform rect = _root.AddComponent<RectTransform>();
		rect.anchorMin = new Vector2(0f, 0.5f);
		rect.anchorMax = new Vector2(0f, 0.5f);
		rect.pivot = new Vector2(0f, 1f);
		rect.sizeDelta = new Vector2(360f, 260f);
		rect.anchoredPosition = new Vector2(28f, -120f);
		((Transform)rect).localScale = Vector3.one;

		_text = _root.AddComponent<TextMeshProUGUI>();
		CopyTmpStyle(sourceTmp, _text);
		((TMP_Text)_text).fontSize = Mathf.Max(16f, ((TMP_Text)sourceTmp).fontSize * 0.85f);
		((TMP_Text)_text).alignment = TextAlignmentOptions.TopLeft;
		((TMP_Text)_text).enableWordWrapping = false;
		((TMP_Text)_text).richText = true;
		((TMP_Text)_text).text = string.Empty;

		_root.SetActive(false);
		_created = true;
		Plugin.Log.LogInfo((object)"[HUD] Held-box list HUD created");
	}

	private void CopyTmpStyle(TextMeshProUGUI source, TextMeshProUGUI dest)
	{
		((TMP_Text)dest).font = ((TMP_Text)source).font;
		((TMP_Text)dest).fontSharedMaterial = ((TMP_Text)source).fontSharedMaterial;
		((TMP_Text)dest).fontSize = ((TMP_Text)source).fontSize;
		((Graphic)dest).color = ((Graphic)source).color;
		((TMP_Text)dest).alpha = ((TMP_Text)source).alpha;
		((TMP_Text)dest).enableWordWrapping = false;
		((TMP_Text)dest).overflowMode = ((TMP_Text)source).overflowMode;
		((TMP_Text)dest).richText = true;
		((Graphic)dest).raycastTarget = false;
		((TMP_Text)dest).horizontalAlignment = HorizontalAlignmentOptions.Left;
		((TMP_Text)dest).verticalAlignment = VerticalAlignmentOptions.Top;
		((TMP_Text)dest).margin = ((TMP_Text)source).margin;
	}

	private void UpdateHudText(BoxInventory inventory)
	{
		if ((Object)(object)_text == (Object)null || (Object)(object)_root == (Object)null)
		{
			return;
		}

		if (!PluginConfig.ShowHud.Value)
		{
			_root.SetActive(false);
			return;
		}

		IQueuableBox held = BoxUtility.GetHeldQueueBox(_holder);
		int queued = inventory?.Count ?? 0;
		int total = (held != null ? 1 : 0) + queued;
		bool visible = total > 0;
		_root.SetActive(visible);
		if (!visible)
		{
			_selectedIndex = 0;
			return;
		}

		_sb.Clear();
		_sb.Append("Holding ").Append(total).Append('/').Append(BoxInventory.MaxQueuedBoxes + 1);

		bool selectMode = PluginConfig.SelectThenConfirm.Value;
		if (selectMode && total > 1)
		{
			_sb.Append("  [").Append(PluginConfig.ConfirmSwitchKey.Value).Append(" switch]");
		}

		_sb.Append('\n');

		if (held != null)
		{
			AppendLine(_sb, 0, BoxDisplay.GetLabel(held), selectMode);
		}

		if (inventory != null)
		{
			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				IQueuableBox box = inventory.QueuedBoxes[i];
				int uiIndex = (held != null ? 1 : 0) + i;
				AppendLine(_sb, uiIndex, BoxDisplay.GetLabel(box), selectMode);
			}
		}

		string text = _sb.ToString();
		if (text != _lastText)
		{
			_lastText = text;
			((TMP_Text)_text).text = text;
		}
	}

	private void AppendLine(StringBuilder sb, int uiIndex, string label, bool selectMode)
	{
		bool isHeld = uiIndex == 0;
		bool isSelected = selectMode && uiIndex == _selectedIndex;
		if (isHeld && !selectMode)
		{
			sb.Append("<color=#FFE28A>► ");
		}
		else if (isSelected)
		{
			sb.Append("<color=#FFE28A>> ");
		}
		else if (isHeld)
		{
			sb.Append("● ");
		}
		else
		{
			sb.Append("  ");
		}

		sb.Append(label);
		if ((isHeld && !selectMode) || isSelected)
		{
			sb.Append("</color>");
		}

		sb.Append('\n');
	}
}
