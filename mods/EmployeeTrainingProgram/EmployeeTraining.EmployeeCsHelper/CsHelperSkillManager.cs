using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

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

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("CsHelper sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		if (PCTrainingApp.Instance != null)
		{
			foreach (CsHelperSkillData data in TrainingData)
			{
				if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.m_CustomerHelpersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				CsHelperSkillData data = TrainingData.FirstOrDefault((CsHelperSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired customer helper id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered customer helper id={id} UI panel");
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<CustomerHelper> active = employeeManager.ActiveCustomerHelpers ?? employeeManager.m_ActiveCustomerHelpers;
		int activeCount = 0;
		if (active != null)
		{
			foreach (CustomerHelper helper in active)
			{
				if (helper != null)
				{
					Spawn(active, helper.CustomerHelperID);
					CsHelperLogic.ApplyRapidity(helper);
					activeCount++;
				}
			}
		}
		Plugin.LogDebug($"CsHelper sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
