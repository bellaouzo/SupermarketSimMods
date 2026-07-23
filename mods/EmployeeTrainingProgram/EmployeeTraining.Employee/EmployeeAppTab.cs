using System;
using System.Collections.Generic;
using EmployeeTraining.Localization;
using EmployeeTraining.TrainingApp;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

using EmployeeTraining;
namespace EmployeeTraining.Employee;

public abstract class EmployeeAppTab<I, S> where I : EmployeeTrainingProgressItem where S : IEmployeeSkill
{
	protected class DetailParam
	{
		public string Name;

		public string LabelKey;

		public string ValueKey;

		public string[] ValueArgs;
	}

	protected class DetailParamWage : DetailParam
	{
		public DetailParamWage()
		{
			Name = "Daily Wage";
			LabelKey = "Daily Wage";
			ValueKey = null;
			ValueArgs = null;
		}
	}

	public GameObject TabObj { get; protected set; }

	public Transform ListObj { get; protected set; }

	public GameObject StatusPanelTmpl { get; protected set; }

	public GameObject NoEmployeeObj { get; protected set; }

	protected string EmployeeNameKey { get; private set; }

	internal GameObject ComposeTabButton(GameObject templateBtnObj, GameObject taskbarBtnsObj, string name, string iconName, string labelKey)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		GameObject val = Object.Instantiate<GameObject>(templateBtnObj, taskbarBtnsObj.transform, true);
		((Object)val).name = name;
		Sprite sprite = UIHelper.ResolveSprite(iconName, UIHelper.IconCashier);
		Transform val2 = val.transform.Find("Tab Icon");
		((Component)val2).GetComponent<Image>().sprite = sprite;
		val2.localPosition = new Vector3(-32f, 0f, 0f);
		MyExtensions.GetOrAddComponent<StringLocalizeTranslator>((Component)(object)val.transform.Find("Text (TMP)")).Key = labelKey;
		((UnityEvent)val.GetComponent<Button>().onClick).AddListener(((UnityAction)(System.Action)(val.GetComponent<MouseClickSFX>().Click)));
		return val;
	}

	protected void CreateStatusPanel(GameObject basePanelObj, GameObject panelTmpl)
	{
		StatusPanelTmpl = Object.Instantiate<GameObject>(basePanelObj, panelTmpl.transform, false);
	}

	protected void ComposeStatusPanel(string iconName, string nameKey, Vector2 detailCellSize, int detailParamCount, DetailParam[] detailParams, Color? bgColorPanel = null, Color? bgColorIntr = null, Color? bgColorExp = null, Color? bgColorRmSlider = null, Color? bgColorRmSeal = null)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Expected O, but got Unknown
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Expected O, but got Unknown
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Expected O, but got Unknown
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_047b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a4: Expected O, but got Unknown
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
		EmployeeNameKey = nameKey;
		GameObject statusPanelTmpl = StatusPanelTmpl;
		GameObject gameObject = ((Component)statusPanelTmpl.transform.Find("Elements/Info")).gameObject;
		Image component = ((Component)gameObject.transform.Find("Icon")).GetComponent<Image>();
		component.sprite = UIHelper.ResolveSprite(iconName, UIHelper.IconCashier);
		GameObject gameObject2 = ((Component)gameObject.transform.Find("Employee Name")).gameObject;
		Object.Destroy((Object)(object)gameObject2.GetComponent<LocalizeStringEvent>());
		StringLocalizeTranslator stringLocalizeTranslator = gameObject2.AddComponent<StringLocalizeTranslator>();
		stringLocalizeTranslator.Key = nameKey;
		object[] args = new string[1] { "<NO>" };
		stringLocalizeTranslator.Translate(args);
		if (bgColorPanel.HasValue)
		{
			Image component2 = ((Component)statusPanelTmpl.transform.Find("BG")).gameObject.GetComponent<Image>();
			((Graphic)component2).color = bgColorPanel.Value;
		}
		if (bgColorIntr.HasValue)
		{
			Image component3 = ((Component)statusPanelTmpl.transform.Find("Interaction Zone")).gameObject.GetComponent<Image>();
			((Graphic)component3).color = bgColorIntr.Value;
		}
		if (bgColorExp.HasValue)
		{
			Image expBackground = ((Component)statusPanelTmpl.transform.Find("Elements/Info/Exp Slider/Background")).gameObject.GetComponent<Image>();
			((Graphic)expBackground).color = bgColorExp.Value;
			Image expFill = ((Component)statusPanelTmpl.transform.Find("Elements/Info/Exp Slider/Fill Area/Fill")).gameObject.GetComponent<Image>();
			((Graphic)expFill).color = UIHelper.Darken(bgColorExp.Value, 0.28f);
		}
		if (bgColorRmSlider.HasValue && bgColorRmSeal.HasValue)
		{
			Grade[] list = Grade.List;
			foreach (Grade grade in list)
			{
				Image component5 = ((Component)statusPanelTmpl.transform.Find("Elements/Info/Roadmap/" + grade.Name + "/Slider/Fill Area/Fill")).gameObject.GetComponent<Image>();
				((Graphic)component5).color = bgColorRmSlider.Value;
				Image component6 = ((Component)statusPanelTmpl.transform.Find("Elements/Info/Roadmap/" + grade.Name + "/Seal/Fill")).gameObject.GetComponent<Image>();
				((Graphic)component6).color = bgColorRmSeal.Value;
			}
		}
		Transform val = statusPanelTmpl.transform.Find("Elements/Info/Detail Params");
		GridLayoutGroup component7 = ((Component)val).GetComponent<GridLayoutGroup>();
		component7.cellSize = detailCellSize;
		component7.constraint = (GridLayoutGroup.Constraint)1;
		component7.constraintCount = detailParamCount;
		component7.spacing = new Vector2(0f, 0f);
		foreach (DetailParam detailParam in detailParams)
		{
			GameObject val2 = Il2CppHelpers.NewGameObject(detailParam.Name, typeof(RectTransform));
			val2.transform.SetParent(((Component)val).transform);
			val2.SetupObject(new Vector3(0f, 0f, 0f));
			GameObject val3 = Il2CppHelpers.NewGameObject("Label", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
			val3.transform.SetParent(val2.transform);
			Vector3 pos = new Vector3(0f, 0f, 0f);
			Vector2 size = new Vector2(80f, 8f);
			Vector2? pivot = new Vector2(0.5f, 1f);
			string labelKey = detailParam.LabelKey;
			Color labelColor = new Color(1f, 1f, 1f, 0.78f);
			val3.SetupText(pos, size, 7.5f, (HorizontalAlignmentOptions)2, labelColor, pivot, labelKey);
			if (detailParam.ValueKey != null)
			{
				GameObject val4 = Il2CppHelpers.NewGameObject("Value", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
				val4.transform.SetParent(val2.transform);
				Vector3 pos2 = new Vector3(0f, -9f, 0f);
				Vector2 size2 = new Vector2(80f, 15f);
				pivot = new Vector2(0.5f, 1f);
				labelKey = detailParam.ValueKey;
				string[] valueArgs = detailParam.ValueArgs;
				val4.SetupText(pos2, size2, 12.5f, (HorizontalAlignmentOptions)2, null, pivot, labelKey, valueArgs);
			}
			else
			{
				GameObject val5 = Il2CppHelpers.NewGameObject("Value", typeof(TextMeshProUGUI));
				val5.transform.SetParent(val2.transform);
				Vector3 pos3 = new Vector3(0f, -9f, 0f);
				Vector2 size3 = new Vector2(80f, 15f);
				pivot = new Vector2(0.5f, 1f);
				val5.SetupText(pos3, size3, 12.5f, (HorizontalAlignmentOptions)2, null, pivot);
			}
		}
	}

	protected void CreateTabScreen(GameObject baseTabObj, GameObject tabsObj)
	{
		TabObj = Object.Instantiate<GameObject>(baseTabObj, tabsObj.transform);
	}

	protected void ComposeTabScreen(string objName, string tabName, Color bgColor)
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		((Object)TabObj).name = objName;
		TabObj.SetActive(false);
		WindowTab component = TabObj.GetComponent<WindowTab>();
		component.TabName = tabName;
		Transform listObj = TabObj.transform.Find("Scroll View/Viewport/Status");
		Transform val = TabObj.transform.Find("Scroll View");
		Image component2 = ((Component)val).GetComponent<Image>();
		((Graphic)component2).color = bgColor;
		ListObj = listObj;
	}

	protected void ComposeNoEmployee(string objName, string textKey, Color textColor)
	{
		GameObject root = Il2CppHelpers.NewGameObject(objName, typeof(RectTransform));
		root.transform.SetParent(TabObj.transform);
		root.SetupObject(new Vector3(0f, 78f, 0f), new Vector2(520f, 70f), new Vector2(0.5f, 0.5f));

		UIHelper.CreateSoftDivider(root.transform, new Vector3(0f, 18f, 0f), 280f);

		Color muted = new Color(textColor.r, textColor.g, textColor.b, Mathf.Clamp01(textColor.a * 0.9f));
		GameObject labelObj = Il2CppHelpers.NewGameObject("Label", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		labelObj.transform.SetParent(root.transform);
		UIHelper.SetupText(
			pos: new Vector3(0f, -4f, 0f),
			size: new Vector2(500f, 36f),
			pivot: new Vector2(0.5f, 0.5f),
			obj: labelObj,
			fontsize: 22f,
			align: (HorizontalAlignmentOptions)2,
			color: muted,
			key: textKey,
			args: null);
		TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
		((TMP_Text)label).characterSpacing = 0.8f;
		((TMP_Text)label).fontStyle = FontStyles.Normal;
		((TMP_Text)label).enableWordWrapping = true;

		root.SetActive(true);
		NoEmployeeObj = root;
	}

	protected void AddTabBtnEvent(GameObject taskbarObj, TabManager tabMgr, string tabBtnPath, string tabName)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		Button component = ((Component)taskbarObj.transform.Find(tabBtnPath)).GetComponent<Button>();
		((UnityEvent)component.onClick).AddListener((UnityAction)delegate
		{
			tabMgr.OpenTab(tabName);
		});
	}

	protected void RegisterEmployee(string panelName, S skill, GameObject unlockApprWindowObj)
	{
		if ((Object)(object)skill.TrainingStatusPanelObj != (Object)null)
		{
			Plugin.LogDebug($"{GetType()}.RegisterEmployee skipped; panel already exists for skill={skill}");
			return;
		}
		Plugin.LogDebug($"{GetType()}.RegisterEmployee: skill={skill}");
		Plugin.LogDebug($"- StatusPanelTmpl={StatusPanelTmpl}, ListObj={ListObj}");
		GameObject val = Object.Instantiate<GameObject>(StatusPanelTmpl, ((Component)ListObj).transform, false);
		Plugin.LogDebug($"- panelObj={val}");
		((Object)val).name = panelName;
		val.SetActive(false);
		I item = val.AddComponent<I>();
		item.Setup(skill, unlockApprWindowObj);
		skill.TrainingStatusPanelObj = val;
		GameObject gameObject = ((Component)val.transform.Find("Elements/Info")).gameObject;
		Plugin.LogDebug($"- infoObj={gameObject}");
		GameObject gameObject2 = ((Component)gameObject.transform.Find("Employee Name")).gameObject;
		Plugin.LogDebug($"- nameObj={gameObject2}");
		StringLocalizeTranslator component = gameObject2.GetComponent<StringLocalizeTranslator>();
		if (component != null)
		{
			component.Key = EmployeeNameKey;
			component.Translate($"{skill.Id}");
		}
		Plugin.LogDebug("- Activating EmployeeProgressItem");
		val.SetActive(true);
		Plugin.LogDebug("- Item setup completed");
		NoEmployeeObj.SetActive(false);
	}

	protected void DeleteEmployee(S skill, List<int> employeeData)
	{
		Object.Destroy((Object)(object)skill.TrainingStatusPanelObj);
		if (employeeData.Count == 0)
		{
			NoEmployeeObj.SetActive(true);
		}
	}

	protected void DeleteEmployee(S skill, Il2CppSystem.Collections.Generic.List<int> employeeData)
	{
		Object.Destroy((Object)(object)skill.TrainingStatusPanelObj);
		if (employeeData.Count == 0)
		{
			NoEmployeeObj.SetActive(true);
		}
	}

	public bool Matches(IEmployeeSkill skill)
	{
		return skill is S;
	}
}
