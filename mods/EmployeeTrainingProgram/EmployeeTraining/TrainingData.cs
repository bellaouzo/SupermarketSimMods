using System;
using System.Collections.Generic;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;

namespace EmployeeTraining;

[Serializable]
public class TrainingData
{
	public List<CashierSkillData> CashierSkills = new List<CashierSkillData>();

	public List<RestockerSkillData> RestockerSkills = new List<RestockerSkillData>();

	public List<CsHelperSkillData> CsHelperSkills = new List<CsHelperSkillData>();

	public List<JanitorSkillData> JanitorSkills = new List<JanitorSkillData>();

	public List<SecuritySkillData> SecuritySkills = new List<SecuritySkillData>();

	public List<BakerSkillData> BakerSkills = new List<BakerSkillData>();

	public List<IceCreamHelperSkillData> IceCreamHelperSkills = new List<IceCreamHelperSkillData>();
}
