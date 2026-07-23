using System;
using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using UnityEngine;

namespace EmployeeTraining.EmployeeCashier;

public class CashierTrainingProgressItem : EmployeeTrainingProgressItem
{
	public CashierTrainingProgressItem(IntPtr ptr) : base(ptr) { }

	private StringLocalizeTranslator spm;

	private StringLocalizeTranslator payment;

	internal override void SetupDetailParams()
	{
		spm = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/SPM/Value")).GetComponent<StringLocalizeTranslator>();
		payment = ((Component)((Component)this).transform.Find("Elements/Info/Detail Params/Payment Time/Value")).GetComponent<StringLocalizeTranslator>();
		if (spm != null)
		{
			spm.Key = "SPM Range";
		}
		if (payment != null)
		{
			payment.Key = "Payment Time Sec.";
		}
		SetDetailLabelKey("SPM", "Scans Per Minute");
		SetDetailLabelKey("Payment Time", "Payment Time");
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
		spm.Translate($"{60f / ((CashierSkill)skill).IntervalMax:0.#}", $"{60f / ((CashierSkill)skill).IntervalMin:0.#}");
		payment.Translate($"{((CashierSkill)skill).TotalCheckoutDuration:0.0#}");
	}
}
