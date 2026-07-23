using System.Collections.Generic;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeSecurity;

public class SecuritySkillManager : EmployeeSkillManager<SecuritySkill, SecuritySkillTier, SecuritySkillData, EmplSecurity, SecurityGuard>
{
	public static SecuritySkillManager Instance;

	internal override List<SecuritySkillData> TrainingData => ETSaveManager.Data.SecuritySkills;

	static SecuritySkillManager()
	{
		Instance = new SecuritySkillManager();
	}

	public override int GetId(SecurityGuard employee)
	{
		return employee.ID;
	}

	public override SecurityGuard Spawn(List<SecurityGuard> employees, int employeeID)
	{
		return base.Spawn(employees, employeeID);
	}
}
