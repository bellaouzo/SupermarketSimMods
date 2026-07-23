using System;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using TMPro;
using UnityEngine;

namespace EmployeeTraining.Localization;

public class StringLocalizeTranslator : MonoBehaviour
{
	public StringLocalizeTranslator(IntPtr ptr)
		: base(ptr)
	{
	}

	public string Key;

	private object[] args = Array.Empty<object>();

	private TMP_Text tmpText;

	private void Awake()
	{
		if (Plugin.Instance != null)
		{
			Plugin.Instance.LocaleChangedEvent += OnLocaleChanged;
		}
		tmpText = GetComponent<TMP_Text>();
	}

	private void Start()
	{
		UpdateText();
	}

	[HideFromIl2Cpp]
	public void Translate(params object[] args)
	{
		this.args = args ?? Array.Empty<object>();
		UpdateText();
	}

	public void UpdateText()
	{
		if (tmpText == null)
		{
			tmpText = GetComponent<TMP_Text>();
		}
		if (tmpText == null || string.IsNullOrEmpty(Key) || Plugin.Localizer == null)
		{
			return;
		}
		try
		{
			object[] resolved = args.Select((object v) => (v is TranslateArgHandler handler) ? handler() : v).ToArray();
			Texts texts = Plugin.Localizer.Get(Key);
			if (texts != null)
			{
				tmpText.text = texts.Translate(resolved);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Translate failed for key '{Key}': {ex.Message}");
		}
	}

	private void OnLocaleChanged(string locale)
	{
		UpdateText();
	}

	private void OnDestroy()
	{
		if (Plugin.Instance != null)
		{
			Plugin.Instance.LocaleChangedEvent -= OnLocaleChanged;
		}
	}
}
