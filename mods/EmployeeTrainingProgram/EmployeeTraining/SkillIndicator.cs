using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Lean.Pool;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmployeeTraining;

public class SkillIndicator : MonoBehaviour
{
	public SkillIndicator(IntPtr ptr) : base(ptr) { }

	private StringLocalizeTranslator lvlText;

	private TMP_Text expText;

	private float orthoCamSmoothness;

	private Transform player;

	private IEmployeeSkill skill;

	private StringLocalizer localizer;

	private const float FILL_SPEED = 1f;

	private Slider expSlider;

	private InGameTextIndicator storePointIndicator;

	private Transform indicatorPosition;

	private bool changingBarForNewLevel;

	private bool fillingBar;

	private Tween fillingTween;

	[HideFromIl2Cpp]
	public void SetUp(IEmployeeSkill skill, StringLocalizer localizer)
	{
		((Behaviour)this).enabled = true;
		this.skill = skill;
		this.localizer = localizer;
	}

	private void Start()
	{
		player = ((Component)Singleton<PlayerController>.Instance).transform;
		orthoCamSmoothness = 0.7f;
		lvlText = ((Component)((Component)this).transform.Find("Lvl Text")).GetComponent<StringLocalizeTranslator>();
		if (lvlText != null)
		{
			lvlText.Key = "Lvl";
		}
		expText = (TMP_Text)(object)((Component)((Component)this).transform.Find("Exp Text")).GetComponent<TextMeshProUGUI>();
		storePointIndicator = Traverse.Create((object)GameObject.Find("---UI---/Ingame Canvas/Store Point Slider").GetComponent<StorePointSlider>()).Field("m_StorePointIndicator").GetValue<InGameTextIndicator>();
		expSlider = ((Component)this).GetComponentInChildren<Slider>(true);
		indicatorPosition = ((Component)expSlider).transform;
		IEmployeeSkill employeeSkill = skill;
		employeeSkill.OnExpChanged = (Action<int, bool>)Delegate.Combine(employeeSkill.OnExpChanged, new Action<int, bool>(ExpChanged));
		IEmployeeSkill employeeSkill2 = skill;
		employeeSkill2.OnLevelChanged = (Action<bool>)Delegate.Combine(employeeSkill2.OnLevelChanged, new Action<bool>(LevelChanged));
		try
		{
			skill.UpdateStatus(init: true);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"SkillIndicator sync failed: {ex.Message}");
		}
		LoadStoreLevelInfo();
	}

	private void LoadStoreLevelInfo()
	{
		if (skill == null)
		{
			return;
		}
		if (lvlText != null)
		{
			lvlText.Key = "Lvl";
			lvlText.Translate(skill.Lvl);
		}
		if (expText != null)
		{
			expText.text = skill.GetExpDisplay();
		}
		UpdateBar();
	}

	private void OnDisable()
	{
	}

	private void Update()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = player.position;
		position.y = ((Component)this).transform.position.y;
		((Component)this).transform.rotation = Quaternion.Slerp(((Component)this).transform.rotation, Quaternion.LookRotation(((Component)this).transform.position - position), orthoCamSmoothness);
	}

	private void ExpChanged(int amount, bool increased)
	{
		if (expText != null)
		{
			expText.text = skill.GetExpDisplay();
		}
		if (amount != 0 && storePointIndicator != null && indicatorPosition != null && localizer != null)
		{
			InGameTextIndicator val = LeanPool.Spawn<InGameTextIndicator>(storePointIndicator, indicatorPosition, false);
			val.Setup(localizer.Get("Popup Exp").Translate(string.Format("{0}{1}", increased ? "+" : "-", amount)), increased);
			((Component)val).transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
		}
		if (changingBarForNewLevel)
		{
			return;
		}
		if (fillingBar)
		{
			Tween obj = fillingTween;
			if (obj != null)
			{
				TweenExtensions.Kill(obj, false);
			}
			fillingTween = null;
			fillingBar = false;
		}
		UpdateBar();
	}

	private void LevelChanged(bool levelUp)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected O, but got Unknown
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		if (lvlText != null)
		{
			lvlText.Key = "Lvl";
			lvlText.Translate(skill.Lvl);
		}
		if (storePointIndicator != null && lvlText != null)
		{
			InGameTextIndicator val = LeanPool.Spawn<InGameTextIndicator>(storePointIndicator, ((Component)lvlText).transform, false);
			val.Setup("LEVEL UP!", true);
			((Component)val).transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
		}
		if (fillingBar)
		{
			Tween obj = fillingTween;
			if (obj != null)
			{
				TweenExtensions.Kill(obj, false);
			}
			fillingTween = null;
			fillingBar = false;
		}
		changingBarForNewLevel = true;
		if (levelUp)
		{
			bool isMax = !skill.GetExpForNext().HasValue;
			float num = (1f - expSlider.value) * 1f;
			TweenerCore<float, float, FloatOptions> val2 = DOTween.To((DOGetter<float>)(() => expSlider.value), (DOSetter<float>)delegate(float x)
			{
				expSlider.value = x;
			}, 1f, num);
			TweenSettingsExtensions.OnComplete<TweenerCore<float, float, FloatOptions>>(val2, (TweenCallback)delegate
			{
				if (!isMax)
				{
					expSlider.value = 0f;
					changingBarForNewLevel = false;
				}
				UpdateBar();
			});
		}
		else
		{
			float num = expSlider.value * 1f;
			TweenerCore<float, float, FloatOptions> val3 = DOTween.To((DOGetter<float>)(() => expSlider.value), (DOSetter<float>)delegate(float x)
			{
				expSlider.value = x;
			}, 0f, num);
			TweenSettingsExtensions.OnComplete<TweenerCore<float, float, FloatOptions>>(val3, (TweenCallback)delegate
			{
				expSlider.value = 1f;
				changingBarForNewLevel = false;
				UpdateBar();
			});
		}
	}

	private void UpdateBar()
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		float? num = skill.GetExpForNext();
		if (num.HasValue)
		{
			fillingBar = true;
			fillingTween = (Tween)(object)DOTween.To((DOGetter<float>)(() => expSlider.value), (DOSetter<float>)delegate(float x)
			{
				expSlider.value = x;
			}, (float)skill.Exp / num.Value, GetDuration());
			TweenSettingsExtensions.OnComplete<Tween>(fillingTween, (TweenCallback)delegate
			{
				fillingBar = false;
			});
		}
		else
		{
			expSlider.value = 1f;
		}
	}

	private float GetDuration()
	{
		return Mathf.Abs((float)skill.Exp / (float)skill.GetExpForNext().Value - expSlider.value) * 1f;
	}

	private void OnDestroy()
	{
		if (skill != null)
		{
			IEmployeeSkill employeeSkill = skill;
			employeeSkill.OnExpChanged = (Action<int, bool>)Delegate.Remove(employeeSkill.OnExpChanged, new Action<int, bool>(ExpChanged));
			IEmployeeSkill employeeSkill2 = skill;
			employeeSkill2.OnLevelChanged = (Action<bool>)Delegate.Remove(employeeSkill2.OnLevelChanged, new Action<bool>(LevelChanged));
			skill = null;
		}
	}
}
