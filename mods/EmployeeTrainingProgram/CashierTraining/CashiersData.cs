using System;
using System.Collections.Generic;
using System.Linq;
using EmployeeTraining;
using EmployeeTraining.EmployeeCashier;

namespace CashierTraining;

[Serializable]
public class CashiersData
{
	public List<CashierSkillData> Skills = new List<CashierSkillData>();

	public TrainingData Migrate()
	{
		return new TrainingData
		{
			CashierSkills = Skills.Select((CashierSkillData s) => new EmployeeTraining.EmployeeCashier.CashierSkillData
			{
				Id = s.Id,
				Exp = s.Exp,
				Grade = s.Grade,
				IsGaugeDisplayed = s.IsGaugeDisplayed
			}).ToList()
		};
	}
}
