using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeRestocker;

public class RestockerTrainingProgressItem : EmployeeTrainingProgressItem
{
	public RestockerTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator rapidity;

	private StringLocalizeTranslator capacity;

	private StringLocalizeTranslator dexterity;

	internal override void SetupDetailParams()
	{
		rapidity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Rapidity/Value")).GetComponent<StringLocalizeTranslator>();
		capacity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Capacity/Value")).GetComponent<StringLocalizeTranslator>();
		dexterity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Dexterity/Value")).GetComponent<StringLocalizeTranslator>();
		if (rapidity != null)
		{
			rapidity.Key = "Speed";
		}
		if (capacity != null)
		{
			capacity.Key = "Weight/Height";
		}
		if (dexterity != null)
		{
			dexterity.Key = "Percentage";
		}
	}

	internal override void UpdateExp()
	{
		base.UpdateExp();
		RestockerSkill typed = (RestockerSkill)skill;
		rapidity.Translate($"{typed.Rapidity:0.0#}");
		capacity.Translate($"{typed.Capacity:0.0#}", $"{typed.CapacityMaxHeight:0.0}");
		dexterity.Translate($"{typed.Dexterity}");
	}
}
