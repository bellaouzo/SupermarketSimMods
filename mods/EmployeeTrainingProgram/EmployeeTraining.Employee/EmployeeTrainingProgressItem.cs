using System;
using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Localization;
using Il2CppInterop.Runtime.Attributes;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EmployeeTraining.Employee;

public class EmployeeTrainingProgressItem : MonoBehaviour
{
	public EmployeeTrainingProgressItem(IntPtr ptr) : base(ptr) { }


	private struct RoadmapObjects
	{
		public GameObject sealObj;

		public GameObject sliderObj;

		public Slider slider;

		public GameObject checkmarkObj;
	}

	internal static readonly StringLocalizer Localizer = Plugin.Localizer;

	internal IEmployeeSkill skill;

	internal GameObject unlockApprovalObj;

	internal Toggle gaugeToggle;

	internal Button trainingBtn;

	internal Button unlockBtn;

	internal TextMeshProUGUI expValue;

	internal Slider expSlider;

	internal StringLocalizeTranslator level;

	internal StringLocalizeTranslator ninjaLabel;

	internal GameObject unlockBtnObj;

	internal GameObject trainBtnObj;

	internal TextMeshProUGUI priceText;

	private TextMeshProUGUI wage;

	private readonly Dictionary<Grade, RoadmapObjects> roadmap = new Dictionary<Grade, RoadmapObjects>();

	private void Awake()
	{
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Expected O, but got Unknown
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Expected O, but got Unknown
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Expected O, but got Unknown
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Expected O, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Expected O, but got Unknown
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Expected O, but got Unknown
		skill.OnExpChanged = (Action<int, bool>)Delegate.Combine(skill.OnExpChanged, new Action<int, bool>(ExpChanged));
		skill.OnLevelChanged = (Action<bool>)Delegate.Combine(skill.OnLevelChanged, new Action<bool>(LevelChanged));
		Plugin.LogDebug("Called EmployeeTrainingProgressItem.Awake");
		MoneyManager instance = Singleton<MoneyManager>.Instance;
		instance.onMoneyTransition += (Il2CppSystem.Action<float, MoneyManager.TransitionType>)(Action<float, MoneyManager.TransitionType>)MoneyChanged;
		Plugin.LogDebug("- TrainingCashierItem.Setup Head Gauge Toggle");
		gaugeToggle = ((Component)((Component)this).transform.Find("Head Gauge Toggle")).GetComponent<Toggle>();
		gaugeToggle.isOn = skill.IsGaugeDisplayed;
		gaugeToggle.onValueChanged = new Toggle.ToggleEvent();
		((UnityEvent<bool>)(object)gaugeToggle.onValueChanged).AddListener((UnityAction<bool>)(Action<bool>)GaugeToggleChanged);
		trainBtnObj = ((Component)((Component)this).transform.Find("Interaction Zone/Training Button")).gameObject;
		trainingBtn = ((Component)((Component)this).transform.Find("Interaction Zone/Training Button")).GetComponent<Button>();
		trainingBtn.onClick = new Button.ButtonClickedEvent();
		((UnityEvent)trainingBtn.onClick).AddListener(((UnityAction)(System.Action)(TrainingBtnClicked)));
		((UnityEvent)trainingBtn.onClick).AddListener(((UnityAction)(System.Action)(trainBtnObj.GetComponent<MouseClickSFX>().Click)));
		unlockBtnObj = ((Component)((Component)this).transform.Find("Interaction Zone/Unlock Button")).gameObject;
		unlockBtn = ((Component)((Component)this).transform.Find("Interaction Zone/Unlock Button")).GetComponent<Button>();
		unlockBtn.onClick = new Button.ButtonClickedEvent();
		((UnityEvent)unlockBtn.onClick).AddListener(((UnityAction)(System.Action)(UnlockBtnClicked)));
		((UnityEvent)unlockBtn.onClick).AddListener(((UnityAction)(System.Action)(unlockBtnObj.GetComponent<MouseClickSFX>().Click)));
		expValue = ((Component)((Component)this).transform.Find("Elements/Info/Exp Value")).GetComponent<TextMeshProUGUI>();
		Plugin.LogDebug($"- expValue: {expValue}");
		expSlider = ((Component)((Component)this).transform.Find("Elements/Info/Exp Slider")).GetComponent<Slider>();
		Plugin.LogDebug($"- expSlider: {expSlider}");
		level = ((Component)((Component)this).transform.Find("Elements/Info/Level")).GetComponent<StringLocalizeTranslator>();
		if (level != null)
		{
			level.Key = "Level";
		}
		Plugin.LogDebug($"- level: {level}");
		wage = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Daily Wage/Value")).GetComponent<TextMeshProUGUI>();
		Plugin.LogDebug($"- wage: {wage}");
		Grade[] list = Grade.List;
		foreach (Grade grade in list)
		{
			GameObject gameObject = ((Component)((Component)this).transform.Find("Elements/Info/Roadmap/" + grade.Name + "/Slider")).gameObject;
			roadmap.Add(grade, new RoadmapObjects
			{
				slider = gameObject.GetComponent<Slider>(),
				sliderObj = gameObject.gameObject,
				sealObj = ((Component)((Component)this).transform.Find("Elements/Info/Roadmap/" + grade.Name + "/Seal")).gameObject,
				checkmarkObj = ((Component)gameObject.transform.Find("Checkmark")).gameObject
			});
			StringLocalizeTranslator gradeLabel = ((Component)((Component)this).transform.Find("Elements/Info/Roadmap/" + grade.Name + "/Label")).GetComponent<StringLocalizeTranslator>();
			if (gradeLabel != null)
			{
				gradeLabel.Key = grade.Name;
			}
		}
		ninjaLabel = ((Component)((Component)this).transform.Find("Elements/Info/Roadmap/" + Grade.Ninja.Name + "/Label")).GetComponent<StringLocalizeTranslator>();
		Plugin.LogDebug($"- ninjaLabel: {ninjaLabel}");
		Transform priceTransform = ((Component)this).transform.Find("Interaction Zone/Price Chip/Total Price Text")
			?? ((Component)this).transform.Find("Interaction Zone/Total Price Text");
		priceText = ((Component)priceTransform).GetComponent<TextMeshProUGUI>();
		RestoreStaticTranslatorKeys();
		SetupDetailParams();
		Plugin.LogDebug("- Called TrainingProgressItem.UpdateExp");
		Plugin.LogDebug("- UpdateExp");
		UpdateExp();
		Plugin.LogDebug("- UpdateLevel");
		UpdateLevel();
		Plugin.LogDebug("Completed setting up TrainingCashierItem");
	}

	[HideFromIl2Cpp]
	public void Setup(IEmployeeSkill skill, GameObject unlockApprovalObj)
	{
		this.skill = skill;
		this.unlockApprovalObj = unlockApprovalObj;
	}

	internal virtual void SetupDetailParams()
	{
	}

	private void RestoreStaticTranslatorKeys()
	{
		SetTranslatorKey("Head Gauge Option", "Show head gauge");
		SetTranslatorKey("Interaction Zone/Training Button/Text", "Train to Level Up");
		SetTranslatorKey("Interaction Zone/Unlock Button/Text", "Unlock Higher Grade");
		SetTranslatorKey("Elements/Info/Exp Label", "Exp.");
		SetTranslatorKey("Elements/Info/Roadmap Title/Text", "Mastery Roadmap");
	}

	private void SetTranslatorKey(string path, string key)
	{
		Transform found = ((Component)this).transform.Find(path);
		if (found == null)
		{
			return;
		}
		StringLocalizeTranslator translator = ((Component)found).GetComponent<StringLocalizeTranslator>();
		if (translator != null)
		{
			translator.Key = key;
			translator.UpdateText();
		}
	}

	private void GaugeToggleChanged(bool toggled)
	{
		skill.IsGaugeDisplayed = toggled;
		skill.ExpGaugeObj.SetActive(toggled);
	}

	private void UnlockBtnClicked()
	{
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		Grade grade = null;
		Grade[] grades = Grade.List;
		for (int i = 0; i < grades.Length; i++)
		{
			if (grades[i].Order == skill.Grade.Order + 1)
			{
				grade = grades[i];
				break;
			}
		}
		if (grade != null)
		{
			StringLocalizeTranslator component = ((Component)unlockApprovalObj.transform.Find("Window BG/Message")).GetComponent<StringLocalizeTranslator>();
			if (component != null)
			{
				component.Key = "Upgrade warning";
			}
			string text = Plugin.Localizer.Get(skill.JobName + " Name").Translate(skill.Id);
			component.Translate(text, Plugin.Localizer.Get(grade.Name).Translate(), Extensions.ToMoneyText(skill.GetWage(grade), 12f, 0.6f));
			GameObject gameObject = ((Component)unlockApprovalObj.transform.Find("Window BG/Approve Button")).gameObject;
			Button component2 = gameObject.GetComponent<Button>();
			component2.onClick = new Button.ButtonClickedEvent();
			((UnityEvent)component2.onClick).AddListener(((UnityAction)(System.Action)(UnlockApproved)));
			((UnityEvent)component2.onClick).AddListener(((UnityAction)(System.Action)(gameObject.GetComponent<MouseClickSFX>().Click)));
			unlockApprovalObj.SetActive(true);
		}
	}

	private void UnlockApproved()
	{
		if (!TrainingNetworkSync.CanGrantExp)
		{
			Plugin.LogInfo("Grade unlock blocked in co-op: only the host can upgrade.");
			return;
		}

		MoneyManager instance = Singleton<MoneyManager>.Instance;
		float? costToUpgrade = skill.GetCostToUpgrade();
		if (costToUpgrade.HasValue && instance.HasMoney(costToUpgrade.Value))
		{
			instance.MoneyTransition(0f - costToUpgrade.Value, (MoneyManager.TransitionType)8, true);
			skill.UnlockGrade();
			UpdateExp();
			UpdateLevel();
			unlockApprovalObj.SetActive(false);
		}
	}

	private void TrainingBtnClicked()
	{
		if (!TrainingNetworkSync.CanGrantExp)
		{
			Plugin.LogInfo("Training blocked in co-op: only the host can spend money to train.");
			return;
		}

		MoneyManager instance = Singleton<MoneyManager>.Instance;
		float? costToLevelup = skill.GetCostToLevelup();
		if (costToLevelup.HasValue && instance.HasMoney(costToLevelup.Value))
		{
			instance.MoneyTransition(0f - costToLevelup.Value, (MoneyManager.TransitionType)8, true);
			skill.TrainToLevelup();
		}
	}

	internal virtual void UpdateExp()
	{
		((TMP_Text)expValue).text = skill.GetExpDisplay();
		int? expForNext = skill.GetExpForNext();
		if (expForNext.HasValue)
		{
			expSlider.value = 1f - (float)skill.Exp / (float)expForNext.Value;
		}
		else
		{
			expSlider.value = 0f;
		}
		((TMP_Text)wage).text = Extensions.ToMoneyText(skill.Wage, 12f, 0.6f);
		unlockBtnObj.SetActive(skill.IsUnlockNeeded());
		trainBtnObj.SetActive(!skill.IsUnlockNeeded());
		float? num = (skill.IsUnlockNeeded() ? skill.GetCostToUpgrade() : skill.GetCostToLevelup());
		if (num.HasValue)
		{
			((TMP_Text)priceText).text = Extensions.ToMoneyText(num.Value, 11f, 0.65f);
		}
		else
		{
			((TMP_Text)priceText).text = "-----";
		}
		if (expForNext.HasValue)
		{
			RoadmapObjects roadmapObjects = roadmap[skill.Grade];
			roadmapObjects.sliderObj.SetActive(true);
			roadmapObjects.slider.value = CalcProgress();
			roadmapObjects.sealObj.SetActive(false);
			roadmapObjects.checkmarkObj.SetActive(false);
		}
	}

	private void UpdateLevel()
	{
		level.Translate(skill.Lvl, (TranslateArgHandler)GetGradeNameArg);
		UpdateButtons();
		foreach (KeyValuePair<Grade, RoadmapObjects> item in roadmap)
		{
			Grade key = item.Key;
			RoadmapObjects value = item.Value;
			bool flag = !skill.GetExpForNext().HasValue;
			if (key < skill.Grade || flag)
			{
				value.sliderObj.SetActive(true);
				value.slider.value = 0f;
				value.checkmarkObj.SetActive(true);
			}
			else if (key == skill.Grade)
			{
				value.sliderObj.SetActive(true);
				value.slider.value = CalcProgress();
				value.sealObj.SetActive(false);
				value.checkmarkObj.SetActive(false);
			}
			else
			{
				value.sliderObj.SetActive(false);
				value.sealObj.SetActive(true);
			}
			if (key == Grade.Ninja)
			{
				ninjaLabel.Key = ((skill.Grade == Grade.Ninja) ? "Ninja" : "?");
				ninjaLabel.Translate();
			}
		}
	}

	[HideFromIl2Cpp]
	private object GetGradeNameArg()
	{
		return Localizer.Get(skill.Grade.Name).Translate();
	}

	private float CalcProgress()
	{
		int? expForNext = skill.GetExpForNext();
		if (expForNext.HasValue)
		{
			return 1f - ((float)skill.Lvl + (float)skill.Exp / (float)expForNext.Value - (float)skill.Grade.LvlMin) / (float)(skill.Grade.LvlMax - skill.Grade.LvlMin + 1);
		}
		return 0f;
	}

	private void UpdateButtons()
	{
		float? costToLevelup = skill.GetCostToLevelup();
		((Selectable)trainingBtn).interactable = costToLevelup.HasValue && Singleton<MoneyManager>.Instance.HasMoney(costToLevelup.Value);
		float? costToUpgrade = skill.GetCostToUpgrade();
		((Selectable)unlockBtn).interactable = costToUpgrade.HasValue && Singleton<MoneyManager>.Instance.HasMoney(costToUpgrade.Value);
	}

	private void ExpChanged(int exp, bool incr)
	{
		UpdateExp();
	}

	private void LevelChanged(bool incr)
	{
		UpdateLevel();
	}

	private void MoneyChanged(float _amount, MoneyManager.TransitionType _type)
	{
		UpdateButtons();
	}

	private void OnDestroy()
	{
		if (skill != null)
		{
			skill.OnExpChanged = (Action<int, bool>)Delegate.Remove(skill.OnExpChanged, new Action<int, bool>(ExpChanged));
			skill.OnLevelChanged = (Action<bool>)Delegate.Remove(skill.OnLevelChanged, new Action<bool>(LevelChanged));
			MoneyManager instance = Singleton<MoneyManager>.Instance;
			if (instance != null)
			{
				instance.onMoneyTransition -= (Il2CppSystem.Action<float, MoneyManager.TransitionType>)(Action<float, MoneyManager.TransitionType>)MoneyChanged;
			}
		}
	}
}
