using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeCsHelper;

public class CsHelperTrainingProgressItem : EmployeeTrainingProgressItem
{
	public CsHelperTrainingProgressItem(IntPtr ptr)
		: base(ptr)
	{
	}

	private StringLocalizeTranslator spm;

	private StringLocalizeTranslator rapidity;

	internal override void SetupDetailParams()
	{
		spm = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/SPM/Value")).GetComponent<StringLocalizeTranslator>();
		rapidity = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Rapidity/Value")).GetComponent<StringLocalizeTranslator>();
		if (spm != null)
		{
			spm.Key = "SPM Range";
		}
		if (rapidity != null)
		{
			rapidity.Key = "Speed";
		}
	}

	internal override void UpdateExp()
	{
		base.UpdateExp();
		CsHelperSkill typed = (CsHelperSkill)skill;
		spm.Translate($"{60f / typed.IntervalMax:0.#}", $"{60f / typed.IntervalMin:0.#}");
		rapidity.Translate($"{typed.Rapidity:0.0#}");
	}
}
