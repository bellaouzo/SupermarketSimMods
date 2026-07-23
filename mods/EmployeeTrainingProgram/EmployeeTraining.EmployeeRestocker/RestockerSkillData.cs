using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeRestocker;

[Serializable]
public class RestockerSkillData : SkillData<RestockerSkill>
{
	public RestockerSkillData()
	{
		Skill = new RestockerSkill(this);
	}
}
