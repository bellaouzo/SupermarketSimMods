using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCashier;

[Serializable]
public class CashierSkillData : SkillData<CashierSkill>
{
	public CashierSkillData()
	{
		Skill = new CashierSkill(this);
	}
}
