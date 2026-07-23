using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SupermarketSimulatorSmartStockOrder;

public sealed class SmartStockOrderMarketButtons : MonoBehaviour
{
	private static readonly List<UnityAction> ButtonActions = new List<UnityAction>();

	private GameObject _fullButton;
	private GameObject _minimumButton;
	private float _nextFindTime;
	private float _nextMissingLogTime;
	private int _missCount;

	public SmartStockOrderMarketButtons(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		bool haveFull = (Object)(object)_fullButton != (Object)null;
		bool haveMin = (Object)(object)_minimumButton != (Object)null;
		if (haveFull)
		{
			_fullButton.SetActive(true);
		}

		if (haveMin)
		{
			_minimumButton.SetActive(true);
		}

		if (haveFull && haveMin)
		{
			return;
		}

		if (Time.unscaledTime < _nextFindTime)
		{
			return;
		}

		float backoff = _missCount < 3 ? 2f : (_missCount < 8 ? 5f : 15f);
		_nextFindTime = Time.unscaledTime + backoff;
		TryInstallButtons();
	}

	private void TryInstallButtons()
	{
		Transform buttonRoot = FindButtonRoot();
		Transform template = ((Object)(object)buttonRoot != (Object)null) ? buttonRoot.Find("Products Tab Button") : null;
		if ((Object)(object)template == (Object)null || (Object)(object)buttonRoot == (Object)null)
		{
			// Never give up permanently: the Market App button root only exists
			// after the player first opens the in-game PC, which can be minutes in.
			_missCount++;
			if (Time.unscaledTime >= _nextMissingLogTime)
			{
				_nextMissingLogTime = Time.unscaledTime + 20f;
				SmartStockOrderPlugin.LogSource.LogInfo("Smart Stock Order: waiting for Market App button template.");
			}

			return;
		}

		_missCount = 0;
		Transform marketRoot = FindMarketAppRoot(template);
		DestroyOrphanSmartStockUi(marketRoot);
		DestroyOrphanSmartStockUi(buttonRoot);
		GameObject templateObject = template.gameObject;
		if ((Object)(object)_fullButton == (Object)null)
		{
			_fullButton = CreateMarketButton(
				templateObject,
				buttonRoot,
				"Fill Racks",
				SmartStockOrderRuntime.OrderFullFromButton);
			_fullButton.name = "Smart Stock Order Refill Button";
		}

		if ((Object)(object)_minimumButton == (Object)null)
		{
			_minimumButton = CreateMarketButton(
				templateObject,
				buttonRoot,
				"1 Box Empty",
				SmartStockOrderRuntime.OrderMinimumFromButton);
			_minimumButton.name = "Smart Stock Order Minimum Button";
		}

		SmartStockOrderPlugin.LogSource.LogInfo("Smart Stock Order: installed Market App buttons in tab row layout.");
	}

	private void DestroyOrphanSmartStockUi(Transform root)
	{
		if ((Object)(object)root == (Object)null)
		{
			return;
		}

		for (int i = root.childCount - 1; i >= 0; i--)
		{
			Transform child = root.GetChild(i);
			if ((Object)(object)child == (Object)null)
			{
				continue;
			}

			string name = child.name;
			bool isOurs = name == "Smart Stock Order Refill Button"
				|| name == "Smart Stock Order Minimum Button"
				|| name == "Smart Stock Order Tooltip";
			if (!isOurs)
			{
				continue;
			}

			GameObject go = child.gameObject;
			if ((Object)(object)go == (Object)(object)_fullButton || (Object)(object)go == (Object)(object)_minimumButton)
			{
				continue;
			}

			Object.Destroy(go);
		}
	}

	private static Transform FindButtonRoot()
	{
		GameObject val = GameObject.Find("---GAME---/Computer/Screen/Market App/Taskbar/Buttons");
		if ((Object)(object)val != (Object)null)
		{
			return val.transform;
		}

		return null;
	}

	private static Transform FindMarketAppRoot(Transform transform)
	{
		Transform val = transform;
		while ((Object)(object)val != (Object)null)
		{
			if (val.name == "Market App")
			{
				return val;
			}

			val = val.parent;
		}

		return null;
	}

	private GameObject CreateMarketButton(GameObject template, Transform parent, string label, Action onClick)
	{
		GameObject val = Object.Instantiate(template, parent, false);
		val.SetActive(true);
		val.transform.SetAsLastSibling();
		LayoutElement layout = val.GetComponent<LayoutElement>();
		if ((Object)(object)layout == (Object)null)
		{
			layout = val.AddComponent<LayoutElement>();
		}

		layout.ignoreLayout = false;
		layout.minWidth = 108f;
		layout.preferredWidth = 118f;
		layout.minHeight = 28f;
		layout.preferredHeight = 32f;
		layout.flexibleWidth = 0f;
		RectTransform component2 = val.GetComponent<RectTransform>();
		if ((Object)(object)component2 != (Object)null)
		{
			float scale = Mathf.Clamp(SmartStockOrderPlugin.MarketButtonScale.Value, 0.45f, 0.85f);
			component2.localScale = new Vector3(scale, scale, 1f);
		}

		LocalizeStringEvent localize = val.GetComponentInChildren<LocalizeStringEvent>(true);
		if ((Object)(object)localize != (Object)null)
		{
			localize.enabled = false;
		}

		TextMeshProUGUI labelText = val.GetComponentInChildren<TextMeshProUGUI>(true);
		if ((Object)(object)labelText != (Object)null)
		{
			labelText.text = label;
			labelText.enableAutoSizing = true;
			labelText.fontSizeMin = 8f;
			labelText.fontSizeMax = 11f;
			labelText.alignment = TextAlignmentOptions.Center;
			labelText.margin = new Vector4(18f, 1f, 4f, 1f);
			labelText.enableWordWrapping = false;
			labelText.overflowMode = TextOverflowModes.Ellipsis;
		}

		RectTransform[] children = val.GetComponentsInChildren<RectTransform>(true);
		foreach (RectTransform child in children)
		{
			if ((Object)(object)child == (Object)null || (Object)(object)child == (Object)(object)component2)
			{
				continue;
			}

			if ((Object)(object)labelText != (Object)null && (Object)(object)child == (Object)(object)labelText.rectTransform)
			{
				child.anchorMin = Vector2.zero;
				child.anchorMax = Vector2.one;
				child.pivot = new Vector2(0.5f, 0.5f);
				child.offsetMin = Vector2.zero;
				child.offsetMax = Vector2.zero;
			}
			else if ((Object)(object)child.parent == (Object)(object)component2)
			{
				string name = child.name.ToLowerInvariant();
				if (!name.Contains("text") && (name.Contains("icon") || name.Contains("image") || name.Contains("tab")))
				{
					child.anchorMin = new Vector2(0f, 0.5f);
					child.anchorMax = new Vector2(0f, 0.5f);
					child.pivot = new Vector2(0.5f, 0.5f);
					child.sizeDelta = new Vector2(14f, 14f);
					child.anchoredPosition = new Vector2(10f, 0f);
				}
			}
		}

		Button button = val.GetComponent<Button>();
		ButtonHandler handler = val.GetComponent<ButtonHandler>();
		if ((Object)(object)handler != (Object)null)
		{
			if ((Object)(object)button != (Object)null)
			{
				button.enabled = false;
			}

			UnityEvent click = new UnityEvent();
			UnityAction action = DelegateSupport.ConvertDelegate<UnityAction>((Delegate)onClick);
			ButtonActions.Add(action);
			click.AddListener(action);
			handler.m_OnClick = click;
		}
		else if ((Object)(object)button != (Object)null)
		{
			button.onClick.RemoveAllListeners();
			UnityAction action = DelegateSupport.ConvertDelegate<UnityAction>((Delegate)onClick);
			ButtonActions.Add(action);
			button.onClick.AddListener(action);
		}

		return val;
	}
}
