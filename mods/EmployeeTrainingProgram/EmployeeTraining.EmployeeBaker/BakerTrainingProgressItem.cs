using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

public class BakerTrainingProgressItem : EmployeeTrainingProgressItem
{
	public BakerTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator rapidity;

	private StringLocalizeTranslator placeSpeed;

	private StringLocalizeTranslator boxHandling;

	internal override void SetupDetailParams()
	{
		rapidity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Rapidity/Value")).GetComponent<StringLocalizeTranslator>();
		placeSpeed = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Place Speed/Value")).GetComponent<StringLocalizeTranslator>();
		boxHandling = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Box Handling/Value")).GetComponent<StringLocalizeTranslator>();
		if (rapidity != null)
		{
			rapidity.Key = "Speed";
		}
		if (placeSpeed != null)
		{
			placeSpeed.Key = "Place Speed Sec.";
		}
		if (boxHandling != null)
		{
			boxHandling.Key = "Box Handling Sec.";
		}
		SetDetailLabelKey("Rapidity", "Walk Speed");
		SetDetailLabelKey("Place Speed", "Place Speed");
		SetDetailLabelKey("Box Handling", "Box Handling");
		SetDetailLabelKey("Daily Wage", "Daily Wage");
	}

	private void SetDetailLabelKey(string paramName, string key)
	{
		Transform found = ((Component)this).transform.Find("Elements/Info/Detail Params/" + paramName + "/Label");
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

	internal override void UpdateExp()
	{
		base.UpdateExp();
		BakerSkill typed = (BakerSkill)skill;
		rapidity.Translate($"{typed.Rapidity:0.0#}");
		placeSpeed.Translate($"{typed.ProductPlacingIntv:0.00}");
		boxHandling.Translate($"{typed.UnpackingTime:0.00}");
	}
}
