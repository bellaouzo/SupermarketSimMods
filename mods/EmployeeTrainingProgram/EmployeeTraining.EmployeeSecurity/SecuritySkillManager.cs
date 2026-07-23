using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

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

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("Security sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		if (PCTrainingApp.Instance != null)
		{
			foreach (SecuritySkillData data in TrainingData)
			{
				if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.m_SecurityGuardsData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				SecuritySkillData data = TrainingData.FirstOrDefault((SecuritySkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired security id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered security id={id} UI panel");
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<SecurityGuard> active = employeeManager.m_ActiveSecurityGuards;
		int activeCount = 0;
		if (active != null)
		{
			foreach (SecurityGuard guard in active)
			{
				if (guard != null)
				{
					Spawn(active, guard.ID);
					activeCount++;
				}
			}
		}
		Plugin.LogInfo($"Security sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
