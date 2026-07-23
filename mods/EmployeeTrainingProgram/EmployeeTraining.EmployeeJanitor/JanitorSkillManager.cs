using System.Collections.Generic;
using System.Linq;
using __Project__.Scripts.Janitor;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

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

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("Janitor sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		if (PCTrainingApp.Instance != null)
		{
			foreach (JanitorSkillData data in TrainingData)
			{
				if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.m_JanitorsData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				JanitorSkillData data = TrainingData.FirstOrDefault((JanitorSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired janitor id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered janitor id={id} UI panel");
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<Janitor> active = employeeManager.ActiveJanitor ?? employeeManager.m_ActiveJanitor;
		int activeCount = 0;
		if (active != null)
		{
			foreach (Janitor janitor in active)
			{
				if (janitor != null)
				{
					Spawn(active, janitor.JanitorID);
					JanitorLogic.ApplyRapidity(janitor);
					activeCount++;
				}
			}
		}
		Plugin.LogInfo($"Janitor sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
