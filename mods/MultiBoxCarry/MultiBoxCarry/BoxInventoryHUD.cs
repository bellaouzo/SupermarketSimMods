using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MultiBoxCarry;

internal sealed class BoxInventoryHUD : MonoBehaviour
{
	private bool _created;

	private GameObject _textGO;

	private GameObject _bgGO;

	private TextMeshProUGUI _text;

	private PlayerObjectHolder holder;

	public BoxInventoryHUD(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		if (!_created)
		{
			TryCreateHud();
		}
		if ((Object)(object)_text != (Object)null)
		{
			UpdateHudText();
		}
	}

	private void TryCreateHud()
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Expected O, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = GameObject.Find("---UI---");
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		Transform val2 = val.transform.Find("Ingame Canvas");
		if ((Object)(object)val2 == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Ingame Canvas not found");
			return;
		}
		Transform val3 = val2.Find("Time");
		Transform val4 = val2.Find("Time BG");
		if ((Object)(object)val3 == (Object)null || (Object)(object)val4 == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Time or Time BG not found");
			return;
		}
		holder = Object.FindObjectOfType<PlayerObjectHolder>();
		if ((Object)(object)holder == (Object)null)
		{
			return;
		}
		TextMeshProUGUI val5 = ((Component)val3).GetComponent<TextMeshProUGUI>() ?? ((Component)val3).GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)val5 == (Object)null)
		{
			Plugin.Log.LogWarning((object)"[HUD] Could not find source TMP on Time");
			return;
		}
		_textGO = new GameObject("InventoryHUD");
		_textGO.transform.SetParent(val2, false);
		RectTransform val6 = _textGO.AddComponent<RectTransform>();
		RectTransform component = ((Component)val5).GetComponent<RectTransform>();
		val6.anchorMin = component.anchorMin;
		val6.anchorMax = component.anchorMax;
		val6.pivot = component.pivot;
		val6.sizeDelta = component.sizeDelta;
		val6.anchoredPosition = component.anchoredPosition + new Vector2(0f, -40f);
		((Transform)val6).localScale = new Vector3(1.2f, 1.2f, 1f);
		_text = _textGO.AddComponent<TextMeshProUGUI>();
		CopyTmpStyle(val5, _text);
		((TMP_Text)_text).text = "0";
		((TMP_Text)_text).alignment = ((TMP_Text)val5).alignment;
		_bgGO = Object.Instantiate<GameObject>(((Component)val4).gameObject, val2);
		((Object)_bgGO).name = "InventoryHUD_BG";
		RectTransform component2 = _bgGO.GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null)
		{
			component2.anchoredPosition = val6.anchoredPosition + new Vector2(20f, 0f);
			((Transform)component2).localScale = new Vector3(0.4f, 0.4f, 1f);
		}
		_bgGO.transform.SetSiblingIndex(_textGO.transform.GetSiblingIndex());
		_textGO.SetActive(false);
		_bgGO.SetActive(false);
		_created = true;
		Plugin.Log.LogInfo((object)"[HUD] Fresh TMP HUD created");
	}

	private void CopyTmpStyle(TextMeshProUGUI source, TextMeshProUGUI dest)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		((TMP_Text)dest).font = ((TMP_Text)source).font;
		((TMP_Text)dest).fontSharedMaterial = ((TMP_Text)source).fontSharedMaterial;
		((TMP_Text)dest).fontSize = ((TMP_Text)source).fontSize;
		((Graphic)dest).color = ((Graphic)source).color;
		((TMP_Text)dest).alpha = ((TMP_Text)source).alpha;
		((TMP_Text)dest).enableWordWrapping = ((TMP_Text)source).enableWordWrapping;
		((TMP_Text)dest).overflowMode = ((TMP_Text)source).overflowMode;
		((TMP_Text)dest).richText = ((TMP_Text)source).richText;
		((Graphic)dest).raycastTarget = false;
		((TMP_Text)dest).horizontalAlignment = ((TMP_Text)source).horizontalAlignment;
		((TMP_Text)dest).verticalAlignment = ((TMP_Text)source).verticalAlignment;
		((TMP_Text)dest).margin = ((TMP_Text)source).margin;
	}

	private void UpdateHudText()
	{
		int num = PlayerInventoryManager.Inventory?.Count ?? 0;
		int num2 = (((Object)(object)holder != (Object)null && (Object)(object)holder.CurrentObject != (Object)null) ? 1 : 0);
		int num3 = num + num2;
		bool active = num3 > 0;
		if ((Object)(object)_textGO != (Object)null)
		{
			_textGO.SetActive(active);
		}
		if ((Object)(object)_bgGO != (Object)null)
		{
			_bgGO.SetActive(active);
		}
		string text = num3.ToString();
		if (((TMP_Text)_text).text != text)
		{
			((TMP_Text)_text).text = text;
		}
	}
}
