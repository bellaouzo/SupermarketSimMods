using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeIceCream;

public class IceCreamHelperTrainingProgressItem : EmployeeTrainingProgressItem
{
	public IceCreamHelperTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator serveSpeed;

	private StringLocalizeTranslator serveRate;

	internal override void SetupDetailParams()
	{
		serveSpeed = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Serve Speed/Value")).GetComponent<StringLocalizeTranslator>();
		serveRate = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Serve Rate/Value")).GetComponent<StringLocalizeTranslator>();
		if (serveSpeed != null)
		{
			serveSpeed.Key = "Serve Speed Sec.";
		}
		if (serveRate != null)
		{
			serveRate.Key = "Serves Per Minute";
		}
		SetDetailLabelKey("Serve Speed", "Serve Speed");
		SetDetailLabelKey("Serve Rate", "Serve Rate");
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
		IceCreamHelperSkill typed = (IceCreamHelperSkill)skill;
		serveSpeed.Translate($"{typed.ActionInterval:0.00}");
		serveRate.Translate($"{typed.ServesPerMinute:0.#}");
	}
}
