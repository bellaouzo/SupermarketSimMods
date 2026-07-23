using EmployeeTraining.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeTraining.TrainingApp;

internal static class UIHelper
{
	public static readonly Material FONT_MATERIAL = Utils.FindResourceByName<Material>("UptownBoy Atlas Material");

	public static readonly TMP_FontAsset FONT_ASSET = Utils.FindResourceByName<TMP_FontAsset>("UptownBoy SDF");

	public static readonly ColorBlock COLOR_BLOCK;

	public static readonly Color TrainButton = new Color(0.16f, 0.58f, 0.46f, 1f);

	public static readonly Color InteractionZone = new Color(0.24f, 0.36f, 0.48f, 0.96f);

	public static readonly Color PriceChip = new Color(0.14f, 0.24f, 0.34f, 0.92f);

	public static readonly Color Divider = new Color(1f, 1f, 1f, 0.22f);

	public static readonly Color CashierPanel = new Color(0.32f, 0.48f, 0.62f, 0.94f);

	public static readonly Color CashierZone = new Color(0.42f, 0.62f, 0.78f, 1f);

	public static readonly Color CashierExp = new Color(0.55f, 0.78f, 0.95f, 1f);

	public static readonly Color CashierSeal = new Color(0.14f, 0.28f, 0.4f, 1f);

	public static readonly Color CsHelperPanel = new Color(0.3f, 0.46f, 0.66f, 0.94f);

	public static readonly Color CsHelperZone = new Color(0.46f, 0.66f, 0.88f, 1f);

	public static readonly Color CsHelperExp = new Color(0.62f, 0.8f, 0.98f, 1f);

	public static readonly Color CsHelperSeal = new Color(0.16f, 0.28f, 0.42f, 1f);

	public const string IconCashier = "icon_shopping_52";

	public const string IconRestocker = "icon_shopping_28";

	public const string IconCsHelper = "icon_shopping_58";

	public const string IconSecurity = "icon_media_48";

	public const string IconJanitor = "Janitor_Icon";

	public const string IconBaker = "bakerIcon";

	public const string IconIceCream = "iceCreamIcon";

	public static readonly Color TabCashierBg = new Color(0.86f, 0.92f, 0.96f, 1f);

	public static readonly Color TabCsHelperBg = new Color(0.88f, 0.93f, 0.98f, 1f);

	public static void SetupObject(this GameObject obj, Vector3 pos, Vector2? size = null, Vector2? pivot = null)
	{
		obj.transform.eulerAngles = new Vector3(0f, 0f, 0f);
		obj.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		obj.transform.localScale = new Vector3(1f, 1f, 1f);
		RectTransform component = obj.GetComponent<RectTransform>();
		if (component != null && size.HasValue)
		{
			component.pivot = pivot ?? new Vector2(0f, 1f);
			component.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.Value.x);
			component.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.Value.y);
			component.anchoredPosition = new Vector2(0f, 0f);
		}
		obj.transform.localPosition = pos;
	}

	public static RectTransform PinTopLeft(GameObject obj, Transform parent, Vector2 anchoredPos, Vector2 size)
	{
		obj.transform.SetParent(parent, false);
		RectTransform rt = obj.GetComponent<RectTransform>();
		if ((Object)(object)rt == (Object)null)
		{
			rt = obj.AddComponent<RectTransform>();
		}

		rt.localScale = Vector3.one;
		rt.localRotation = Quaternion.identity;
		rt.anchorMin = new Vector2(0f, 1f);
		rt.anchorMax = new Vector2(0f, 1f);
		rt.pivot = new Vector2(0f, 1f);
		rt.sizeDelta = size;
		rt.anchoredPosition = anchoredPos;
		return rt;
	}

	public static void PinStatusList(Transform status)
	{
		if ((Object)(object)status == (Object)null)
		{
			return;
		}

		GridLayoutGroup grid = status.GetComponent<GridLayoutGroup>();
		if ((Object)(object)grid != (Object)null)
		{
			grid.cellSize = new Vector2(320f, 145f);
			grid.constraint = GridLayoutGroup.Constraint.Flexible;
			grid.spacing = new Vector2(10f, 10f);
			grid.padding = new RectOffset(0, 0, 0, 0);
			grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
			grid.startAxis = GridLayoutGroup.Axis.Horizontal;
			grid.childAlignment = TextAnchor.UpperLeft;
		}

		status.localPosition = new Vector3(0f, 95f, 0f);
	}

	public static void SetupText(this GameObject obj, Vector3 pos, Vector2 size, float fontsize, HorizontalAlignmentOptions align = HorizontalAlignmentOptions.Left, Color? color = null, Vector2? pivot = null, string key = null, string[] args = null)
	{
		obj.SetupObject(pos, size, pivot);
		TextMeshProUGUI component = obj.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component).fontSize = fontsize;
		((TMP_Text)component).fontSizeMax = fontsize;
		((TMP_Text)component).fontSizeMin = 0f;
		((Graphic)component).color = color ?? new Color(1f, 1f, 1f);
		((TMP_Text)component).horizontalAlignment = align;
		((TMP_Text)component).verticalAlignment = VerticalAlignmentOptions.Middle;
		((TMP_Text)component).fontMaterial = FONT_MATERIAL;
		((TMP_Text)component).font = FONT_ASSET;
		((TMP_Text)component).autoSizeTextContainer = false;
		((TMP_Text)component).enableAutoSizing = true;
		((TMP_Text)component).enableWordWrapping = false;
		if (key != null)
		{
			StringLocalizeTranslator component2 = obj.GetComponent<StringLocalizeTranslator>();
			component2.Key = key;
			if (args != null)
			{
				component2.Translate(args);
			}
		}
	}

	public static void SetRawText(this GameObject baseObj, string name, string text)
	{
		GameObject gameObject = ((Component)baseObj.transform.Find(name)).gameObject;
		((TMP_Text)gameObject.GetComponent<TextMeshProUGUI>()).text = text;
	}

	public static void StyleActionButton(Image image, Button button, Color fill)
	{
		if ((Object)(object)image != (Object)null)
		{
			((Graphic)image).color = fill;
		}

		if ((Object)(object)button != (Object)null)
		{
			((Selectable)button).colors = COLOR_BLOCK;
		}

		Outline outline = ((Component)image).GetComponent<Outline>();
		if ((Object)(object)outline == (Object)null)
		{
			outline = ((Component)image).gameObject.AddComponent<Outline>();
		}

		((Shadow)outline).effectColor = new Color(0f, 0f, 0f, 0.45f);
		((Shadow)outline).effectDistance = new Vector2(0.4f, -0.4f);
	}

	public static GameObject CreateSoftDivider(Transform parent, Vector3 pos, float width)
	{
		GameObject divider = Il2CppHelpers.NewGameObject("Divider", typeof(RawImage));
		divider.transform.SetParent(parent);
		divider.SetupObject(pos, new Vector2(width, 1.5f), new Vector2(0.5f, 0.5f));
		RawImage image = divider.GetComponent<RawImage>();
		image.texture = (Texture)(object)Utils.FindResourceByName<Texture2D>("UnityWhite");
		((Graphic)image).color = Divider;
		return divider;
	}

	public static Sprite ResolveSprite(params string[] candidates)
	{
		if (candidates == null)
		{
			return null;
		}

		foreach (string name in candidates)
		{
			if (string.IsNullOrEmpty(name))
			{
				continue;
			}

			Sprite sprite = Utils.FindResourceByName<Sprite>(name);
			if ((Object)(object)sprite != (Object)null)
			{
				return sprite;
			}
		}

		return null;
	}

	public static Color Darken(Color color, float factor)
	{
		factor = Mathf.Clamp01(factor);
		return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
	}

	static UIHelper()
	{
		ColorBlock block = default(ColorBlock);
		block.colorMultiplier = 1f;
		block.fadeDuration = 0.08f;
		block.normalColor = new Color(1f, 1f, 1f, 1f);
		block.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
		block.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
		block.selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
		block.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.45f);
		COLOR_BLOCK = block;
	}
}
