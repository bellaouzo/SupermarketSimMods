using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeJanitor;

[Serializable]
public class JanitorSkillData : SkillData<JanitorSkill>
{
	public JanitorSkillData()
	{
		Skill = new JanitorSkill(this);
	}
}
