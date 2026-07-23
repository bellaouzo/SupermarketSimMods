using System.Collections.Generic;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCsHelper;

public class CsHelperSkillManager : EmployeeSkillManager<CsHelperSkill, CsHelperSkillTier, CsHelperSkillData, EmplCsHelper, CustomerHelper>
{
	public static CsHelperSkillManager Instance;

	internal override List<CsHelperSkillData> TrainingData => ETSaveManager.Data.CsHelperSkills;

	static CsHelperSkillManager()
	{
		Instance = new CsHelperSkillManager();
	}

	public override int GetId(CustomerHelper employee)
	{
		return employee.CustomerHelperID;
	}

	public override CustomerHelper Spawn(List<CustomerHelper> employees, int employeeID)
	{
		CustomerHelper val = base.Spawn(employees, employeeID);
		CsHelperLogic.ApplyRapidity(val);
		return val;
	}
}
