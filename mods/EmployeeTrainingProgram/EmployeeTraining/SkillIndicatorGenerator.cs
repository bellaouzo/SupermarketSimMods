using System;
using EmployeeTraining.Localization;
using EmployeeTraining.TrainingApp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeTraining;

public static class SkillIndicatorGenerator
{
	public static GameObject SkillIndicatorTmpl { get; private set; }

	public static void Init()
	{
		Plugin instance = Plugin.Instance;
		instance.GameLoadedEvent = (Action)Delegate.Combine(instance.GameLoadedEvent, new Action(GenerateTemplate));
		Plugin instance2 = Plugin.Instance;
		instance2.GameQuitEvent = (Action)Delegate.Combine(instance2.GameQuitEvent, new Action(Dispose));
	}

	private static void GenerateTemplate()
	{
		Color panelColor = new Color(0.07f, 0.09f, 0.11f, 0.82f);
		Color trackColor = new Color(1f, 1f, 1f, 0.12f);
		Color fillColor = new Color(0.35f, 0.82f, 0.62f, 1f);
		Color levelColor = new Color(0.95f, 0.97f, 0.98f, 1f);
		Color expColor = new Color(0.78f, 0.84f, 0.86f, 0.95f);

		GameObject root = Il2CppHelpers.NewGameObject("Skill Indicator", typeof(SkillIndicator), typeof(Canvas));
		Canvas canvas = root.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		canvas.additionalShaderChannels = (AdditionalCanvasShaderChannels)25;

		RectTransform rootRect = root.GetComponent<RectTransform>();
		rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0.72f);
		rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.22f);
		root.transform.localScale = new Vector3(1f, 1f, 1f);

		GameObject panel = Il2CppHelpers.NewGameObject("Panel", typeof(CanvasRenderer), typeof(Image));
		panel.transform.SetParent(root.transform, false);
		Image panelImage = panel.GetComponent<Image>();
		((Graphic)panelImage).color = panelColor;
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = new Vector2(0.5f, 0.5f);
		panelRect.anchorMax = new Vector2(0.5f, 0.5f);
		panelRect.pivot = new Vector2(0.5f, 0.5f);
		panelRect.anchoredPosition = Vector2.zero;
		panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0.7f);
		panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.18f);

		GameObject sliderObj = Il2CppHelpers.NewGameObject("Exp Slider", typeof(Slider));
		sliderObj.transform.SetParent(root.transform, false);
		sliderObj.transform.localPosition = new Vector3(0f, -0.025f, 0f);

		GameObject background = Il2CppHelpers.NewGameObject("Background", typeof(CanvasRenderer), typeof(Image));
		background.transform.SetParent(sliderObj.transform, false);
		Image backgroundImage = background.GetComponent<Image>();
		((Graphic)backgroundImage).color = trackColor;

		GameObject fillArea = Il2CppHelpers.NewGameObject("Fill Area", typeof(RectTransform));
		fillArea.transform.SetParent(sliderObj.transform, false);

		GameObject fill = Il2CppHelpers.NewGameObject("Fill", typeof(CanvasRenderer), typeof(Image));
		fill.transform.SetParent(fillArea.transform, false);
		Image fillImage = fill.GetComponent<Image>();
		((Graphic)fillImage).color = fillColor;

		Vector2 barPivot = new Vector2(0.5f, 0.5f);
		Vector2 barSize = new Vector2(0.58f, 0.045f);

		RectTransform backgroundRect = background.GetComponent<RectTransform>();
		backgroundRect.pivot = barPivot;
		backgroundRect.anchoredPosition = Vector2.zero;
		backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barSize.x);
		backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barSize.y);

		RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
		fillAreaRect.pivot = barPivot;
		fillAreaRect.anchoredPosition = Vector2.zero;
		fillAreaRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barSize.x);
		fillAreaRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, barSize.y);

		RectTransform fillRect = fill.GetComponent<RectTransform>();
		fillRect.anchorMin = new Vector2(0f, 0f);
		fillRect.anchorMax = new Vector2(1f, 1f);
		fillRect.offsetMin = new Vector2(0.008f, 0.006f);
		fillRect.offsetMax = new Vector2(-0.008f, -0.006f);

		Slider slider = sliderObj.GetComponent<Slider>();
		slider.fillRect = fillRect;
		((Selectable)slider).interactable = false;
		((Selectable)slider).transition = Selectable.Transition.None;

		RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
		sliderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		sliderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

		GameObject lvlTextObj = Il2CppHelpers.NewGameObject("Lvl Text", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator)).gameObject;
		lvlTextObj.transform.SetParent(root.transform, false);
		TextMeshProUGUI lvlText = lvlTextObj.GetComponent<TextMeshProUGUI>();
		((TMP_Text)lvlText).fontSize = 0.085f;
		((TMP_Text)lvlText).fontSizeMax = 0.085f;
		((TMP_Text)lvlText).fontSizeMin = 0f;
		((Graphic)lvlText).color = levelColor;
		((TMP_Text)lvlText).horizontalAlignment = HorizontalAlignmentOptions.Left;
		((TMP_Text)lvlText).verticalAlignment = VerticalAlignmentOptions.Middle;
		((TMP_Text)lvlText).fontMaterial = UIHelper.FONT_MATERIAL;
		((TMP_Text)lvlText).font = UIHelper.FONT_ASSET;
		((TMP_Text)lvlText).autoSizeTextContainer = true;
		((TMP_Text)lvlText).enableAutoSizing = false;
		((TMP_Text)lvlText).enableWordWrapping = false;
		((TMP_Text)lvlText).outlineWidth = 0.15f;
		((TMP_Text)lvlText).outlineColor = new Color32(0, 0, 0, 160);
		StringLocalizeTranslator translator = lvlTextObj.GetComponent<StringLocalizeTranslator>();
		translator.Key = "Lvl";
		translator.Translate("<LVL>");
		RectTransform lvlRect = lvlTextObj.GetComponent<RectTransform>();
		lvlRect.pivot = new Vector2(0f, 0.5f);
		lvlRect.anchoredPosition = new Vector2(-0.3f, 0.045f);
		lvlRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		lvlRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

		GameObject expTextObj = Il2CppHelpers.NewGameObject("Exp Text", typeof(TextMeshProUGUI)).gameObject;
		expTextObj.transform.SetParent(root.transform, false);
		TextMeshProUGUI expText = expTextObj.GetComponent<TextMeshProUGUI>();
		((TMP_Text)expText).fontMaterial = UIHelper.FONT_MATERIAL;
		((TMP_Text)expText).font = UIHelper.FONT_ASSET;
		((TMP_Text)expText).fontSize = 0.065f;
		((TMP_Text)expText).fontSizeMax = 0.065f;
		((TMP_Text)expText).fontSizeMin = 0f;
		((Graphic)expText).color = expColor;
		((TMP_Text)expText).outlineColor = new Color32(0, 0, 0, 140);
		((TMP_Text)expText).outlineWidth = 0.12f;
		((TMP_Text)expText).horizontalAlignment = HorizontalAlignmentOptions.Right;
		((TMP_Text)expText).verticalAlignment = VerticalAlignmentOptions.Middle;
		((TMP_Text)expText).autoSizeTextContainer = true;
		((TMP_Text)expText).enableAutoSizing = false;
		((TMP_Text)expText).enableWordWrapping = false;
		RectTransform expRect = expTextObj.GetComponent<RectTransform>();
		expRect.pivot = new Vector2(1f, 0.5f);
		expRect.anchoredPosition = new Vector2(0.3f, 0.045f);
		expRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		expRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

		root.SetActive(false);
		SkillIndicatorTmpl = root;
	}

	public static void Dispose()
	{
		SkillIndicatorTmpl = null;
	}
}
