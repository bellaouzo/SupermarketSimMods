using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeBaker;

[Serializable]
public class BakerSkillData : SkillData<BakerSkill>
{
	public BakerSkillData()
	{
		Skill = new BakerSkill(this);
	}
}
