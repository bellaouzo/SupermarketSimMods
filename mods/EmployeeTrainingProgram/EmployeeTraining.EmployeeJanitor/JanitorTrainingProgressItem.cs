using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeJanitor;

public class JanitorTrainingProgressItem : EmployeeTrainingProgressItem
{
	public JanitorTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator dexterity;

	private StringLocalizeTranslator rapidity;

	internal override void SetupDetailParams()
	{
		rapidity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Rapidity/Value")).GetComponent<StringLocalizeTranslator>();
		dexterity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Dexterity/Value")).GetComponent<StringLocalizeTranslator>();
		if (rapidity != null)
		{
			rapidity.Key = "Speed";
		}
		if (dexterity != null)
		{
			dexterity.Key = "Percentage";
		}
	}

	internal override void UpdateExp()
	{
		base.UpdateExp();
		JanitorSkill typed = (JanitorSkill)skill;
		rapidity.Translate($"{typed.Rapidity:0.0#}");
		dexterity.Translate($"{typed.Dexterity}");
	}
}
