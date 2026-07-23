using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeSecurity;

public class SecurityTrainingProgressItem : EmployeeTrainingProgressItem
{
	public SecurityTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator repidity;

	private StringLocalizeTranslator dexterity;

	internal override void SetupDetailParams()
	{
		repidity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Rapidity/Value")).GetComponent<StringLocalizeTranslator>();
		dexterity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Dexterity/Value")).GetComponent<StringLocalizeTranslator>();
		if (repidity != null)
		{
			repidity.Key = "Speed";
		}
		if (dexterity != null)
		{
			dexterity.Key = "Percentage";
		}
	}

	internal override void UpdateExp()
	{
		base.UpdateExp();
		SecuritySkill typed = (SecuritySkill)skill;
		repidity.Translate($"{typed.Rapidity:0.0#}");
		dexterity.Translate($"{typed.Dexterity}");
	}
}
