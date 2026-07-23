using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCsHelper;

[Serializable]
public class CsHelperSkillData : SkillData<CsHelperSkill>
{
	public CsHelperSkillData()
	{
		Skill = new CsHelperSkill(this);
	}
}
