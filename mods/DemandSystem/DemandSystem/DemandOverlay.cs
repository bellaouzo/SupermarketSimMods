using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DemandSystem;

internal sealed class DemandOverlay : MonoBehaviour
{
	private static DemandOverlay _instance;

	private GameObject _panelObject;
	private RectTransform _panelRect;
	private RectTransform _rowsRoot;
	private Font _font;
	private string _renderedKey = string.Empty;
	private Text _titleText;
	private Text _subtitleText;
	private bool _wasGameplay;
	private float _gameplayReadyAt = -1f;
	private bool _legacyShadowRemoved;
	private string _cachedSceneName = string.Empty;
	private bool _cachedGameplay;
	private float _nextIdleCheckAt;

	public DemandOverlay(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static void Create()
	{
		if ((Object)(object)_instance != (Object)null)
		{
			return;
		}

		GameObject go = new GameObject("DemandSystemOverlay");
		Object.DontDestroyOnLoad((Object)go);
		((Object)go).hideFlags = HideFlags.HideAndDontSave;
		_instance = go.AddComponent<DemandOverlay>();
	}

	private void Awake()
	{
		BuildCanvas();
	}

	private void Update()
	{
		if ((Object)(object)_panelObject == (Object)null)
		{
			BuildCanvas();
		}

		if (!_legacyShadowRemoved)
		{
			RemoveLegacyShadow();
			_legacyShadowRemoved = true;
		}

		bool panelActive = _panelObject.activeSelf;
		if (!panelActive && Time.unscaledTime < _nextIdleCheckAt)
		{
			return;
		}

		bool gameplay = IsGameplaySceneCached();
		if (gameplay && !_wasGameplay && DemandState.HasActiveDemand)
		{
			DemandState.RequestOverlay();
		}

		if (!gameplay)
		{
			_gameplayReadyAt = -1f;
		}
		else if (DemandState.OverlayNeedsSettle)
		{
			DemandState.OverlayNeedsSettle = false;
			_gameplayReadyAt = Time.unscaledTime + 1.5f;
		}

		_wasGameplay = gameplay;

		bool settled = gameplay && (_gameplayReadyAt < 0f || Time.unscaledTime >= _gameplayReadyAt);
		bool show = DemandState.ShouldShowOverlay && settled;
		if (panelActive != show)
		{
			_panelObject.SetActive(show);
			panelActive = show;
		}

		if (!show)
		{
			_nextIdleCheckAt = Time.unscaledTime + 0.25f;
			return;
		}

		DemandState.ConsumeOverlayTime(Time.unscaledDeltaTime);

		string key = DemandText.CurrentLocaleCode() + "|" + string.Join("|", DemandState.DemandedProducts);
		if (_renderedKey != key)
		{
			RebuildRows();
			_renderedKey = key;
		}
	}

	private void BuildCanvas()
	{
		RemoveLegacyShadow();
		Canvas canvas = ((Component)this).gameObject.GetComponent<Canvas>();
		if ((Object)(object)canvas == (Object)null)
		{
			canvas = ((Component)this).gameObject.AddComponent<Canvas>();
		}

		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 32767;

		CanvasScaler scaler = ((Component)this).gameObject.GetComponent<CanvasScaler>();
		if ((Object)(object)scaler == (Object)null)
		{
			scaler = ((Component)this).gameObject.AddComponent<CanvasScaler>();
		}

		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		if ((Object)(object)((Component)this).gameObject.GetComponent<GraphicRaycaster>() == (Object)null)
		{
			((Component)this).gameObject.AddComponent<GraphicRaycaster>();
		}

		_font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		if ((Object)(object)_panelObject != (Object)null)
		{
			return;
		}

		_panelObject = CreateImage("Panel", ((Component)this).transform, new Color(0.045f, 0.052f, 0.058f, 1f));
		_panelRect = _panelObject.GetComponent<RectTransform>();
		AnchorTopRight(_panelRect, new Vector2(-24f, -108f), new Vector2(390f, 244f));
		AnchorLeftStretch(CreateImage("Accent", _panelObject.transform, new Color(0.08f, 0.75f, 0.45f, 1f)).GetComponent<RectTransform>(), 0f, 0f, 5f);

		OverlayText text = DemandText.Current();
		_titleText = CreateText("Title", _panelObject.transform, text.Title, 18, FontStyle.Bold, Color.white);
		AnchorTopLeft(((Graphic)_titleText).rectTransform, new Vector2(18f, -12f), new Vector2(350f, 24f));
		_subtitleText = CreateText("Subtitle", _panelObject.transform, text.Subtitle, 14, FontStyle.Normal, new Color(0.78f, 0.84f, 0.82f, 1f));
		AnchorTopLeft(((Graphic)_subtitleText).rectTransform, new Vector2(18f, -36f), new Vector2(350f, 22f));

		GameObject rows = new GameObject("Rows");
		rows.transform.SetParent(_panelObject.transform, false);
		_rowsRoot = rows.AddComponent<RectTransform>();
		AnchorTopLeft(_rowsRoot, new Vector2(12f, -64f), new Vector2(366f, 174f));
		_panelObject.SetActive(false);
	}

	private void RemoveLegacyShadow()
	{
		Transform shadow = ((Component)this).transform.Find("Shadow");
		if ((Object)(object)shadow != (Object)null)
		{
			Object.Destroy((Object)((Component)shadow).gameObject);
		}
	}

	private bool IsGameplaySceneCached()
	{
		Scene scene = SceneManager.GetActiveScene();
		string name = scene.name ?? string.Empty;
		if (name == _cachedSceneName)
		{
			return _cachedGameplay;
		}

		_cachedSceneName = name;
		_cachedGameplay = !(name.Contains("Menu", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("Title", StringComparison.OrdinalIgnoreCase)
			|| name.Contains("Loading", StringComparison.OrdinalIgnoreCase));
		return _cachedGameplay;
	}

	private void RebuildRows()
	{
		OverlayText text = DemandText.Current();
		_titleText.text = text.Title;
		_subtitleText.text = text.Subtitle;

		for (int i = ((Transform)_rowsRoot).childCount - 1; i >= 0; i--)
		{
			Object.Destroy((Object)((Component)((Transform)_rowsRoot).GetChild(i)).gameObject);
		}

		float rowHeight = 58f;
		int count = DemandState.DemandedProducts.Count;
		_panelRect.sizeDelta = new Vector2(390f, 70f + count * rowHeight);
		_rowsRoot.sizeDelta = new Vector2(366f, count * rowHeight);

		for (int i = 0; i < count; i++)
		{
			int productId = DemandState.DemandedProducts[i];
			GameObject row = CreateImage($"Row {i + 1}", (Transform)_rowsRoot, new Color(0.08f, 0.095f, 0.105f, 1f));
			AnchorTopLeft(row.GetComponent<RectTransform>(), new Vector2(0f, -i * rowHeight), new Vector2(366f, 52f));

			GameObject iconGo = CreateImage("Icon", row.transform, new Color(0.16f, 0.18f, 0.19f, 1f));
			AnchorTopLeft(iconGo.GetComponent<RectTransform>(), new Vector2(6f, -4f), new Vector2(44f, 44f));
			Image icon = iconGo.GetComponent<Image>();
			Sprite sprite = DemandState.ProductSprite(productId);
			if ((Object)(object)sprite != (Object)null)
			{
				icon.sprite = sprite;
				icon.preserveAspect = true;
				((Graphic)icon).color = Color.white;
			}

			AnchorTopLeft(((Graphic)CreateText("Name", row.transform, DemandState.ProductName(productId), 17, FontStyle.Bold, Color.white)).rectTransform, new Vector2(62f, -6f), new Vector2(290f, 24f));
			AnchorTopLeft(((Graphic)CreateText("Note", row.transform, text.RowNote, 13, FontStyle.Normal, new Color(0.78f, 0.84f, 0.82f, 1f))).rectTransform, new Vector2(62f, -30f), new Vector2(290f, 20f));
		}
	}

	private GameObject CreateImage(string name, Transform parent, Color color)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		Image image = go.AddComponent<Image>();
		((Graphic)image).color = color;
		((Graphic)image).raycastTarget = false;
		return go;
	}

	private Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, Color color)
	{
		GameObject go = new GameObject(name);
		go.transform.SetParent(parent, false);
		Text text = go.AddComponent<Text>();
		text.text = value;
		text.font = _font;
		text.fontSize = fontSize;
		text.fontStyle = fontStyle;
		((Graphic)text).color = color;
		text.alignment = TextAnchor.MiddleLeft;
		((Graphic)text).raycastTarget = false;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		return text;
	}

	private static void AnchorTopRight(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
	{
		rect.anchorMin = new Vector2(1f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(1f, 1f);
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = size;
	}

	private static void AnchorTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
	{
		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 1f);
		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = size;
	}

	private static void AnchorLeftStretch(RectTransform rect, float left, float top, float width)
	{
		rect.anchorMin = new Vector2(0f, 0f);
		rect.anchorMax = new Vector2(0f, 1f);
		rect.pivot = new Vector2(0f, 0.5f);
		rect.offsetMin = new Vector2(left, 0f);
		rect.offsetMax = new Vector2(left + width, top);
	}
}
