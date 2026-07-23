using __Project__.Scripts.Janitor;
using System.Collections.Generic;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeJanitor;

public class JanitorSkillManager : EmployeeSkillManager<JanitorSkill, JanitorSkillTier, JanitorSkillData, EmplJanitor, Janitor>
{
	public static JanitorSkillManager Instance;

	internal override List<JanitorSkillData> TrainingData => ETSaveManager.Data.JanitorSkills;

	static JanitorSkillManager()
	{
		Instance = new JanitorSkillManager();
	}

	public override int GetId(Janitor employee)
	{
		return employee.JanitorID;
	}

	public override Janitor Spawn(List<Janitor> employees, int employeeID)
	{
		Janitor val = base.Spawn(employees, employeeID);
		JanitorLogic.ApplyRapidity(val);
		return val;
	}
}
