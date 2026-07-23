using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeSecurity;

[Serializable]
public class SecuritySkillData : SkillData<SecuritySkill>
{
	public SecuritySkillData()
	{
		Skill = new SecuritySkill(this);
	}
}
