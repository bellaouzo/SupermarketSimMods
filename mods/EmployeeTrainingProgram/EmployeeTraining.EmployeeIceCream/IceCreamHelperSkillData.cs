using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeIceCream;

[Serializable]
public class IceCreamHelperSkillData : SkillData<IceCreamHelperSkill>
{
	public IceCreamHelperSkillData()
	{
		Skill = new IceCreamHelperSkill(this);
	}
}
