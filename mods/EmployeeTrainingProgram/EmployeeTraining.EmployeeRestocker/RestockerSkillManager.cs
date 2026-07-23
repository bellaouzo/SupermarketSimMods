using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;
using Clerk = SupermarketSimulator.Clerk.Clerk;

namespace EmployeeTraining.EmployeeRestocker;

public class RestockerSkillManager : EmployeeSkillManager<RestockerSkill, RestockerSkillTier, RestockerSkillData, EmplRestocker, Restocker>
{
	public static RestockerSkillManager Instance;

	internal override List<RestockerSkillData> TrainingData => ETSaveManager.Data.RestockerSkills;

	static RestockerSkillManager()
	{
		Instance = new RestockerSkillManager();
	}

	public List<RestockerLogic> GetActiveLogics()
	{
		return (from d in TrainingData
			where d.Skill.IsAssigned()
			select d.Skill.Logic).ToList();
	}

	public override int GetId(Restocker employee)
	{
		return employee.RestockerID;
	}

	public RestockerSkill GetSkillById(int id)
	{
		RestockerSkillData data = TrainingData.FirstOrDefault((RestockerSkillData c) => c.Id == id);
		return data?.Skill;
	}

	public RestockerSkill AssignClerk(Clerk clerk)
	{
		if (clerk == null)
		{
			return null;
		}
		RestockerSkill skill = Register(clerk.EmployeeId);
		TryAttachGauge(skill, clerk);
		ClerkLogic.ApplyRapidity(clerk);
		return skill;
	}

	public void TryAttachGauge(RestockerSkill skill, Clerk clerk)
	{
		if (skill == null || clerk == null || SkillIndicatorGenerator.SkillIndicatorTmpl == null)
		{
			return;
		}
		if ((Object)(object)skill.ExpGaugeObj != (Object)null)
		{
			return;
		}
		if ((Object)(object)((Component)clerk).GetComponentInChildren<SkillIndicator>(true) != (Object)null)
		{
			return;
		}
		Plugin.LogInfo($"Adding Skill Indicator for Clerk/Restocker {clerk.EmployeeId}");
		GameObject gauge = Object.Instantiate(SkillIndicatorGenerator.SkillIndicatorTmpl, ((Component)clerk).transform, false);
		SkillIndicator indicator = gauge.GetComponent<SkillIndicator>();
		((Component)indicator).transform.localPosition = new Vector3(0.18f, Plugin.Instance.Settings.GaugeHeight, 0f);
		((Component)indicator).gameObject.SetActive(true);
		indicator.SetUp(skill, Plugin.Localizer);
		skill.ExpGaugeObj = gauge;
		gauge.SetActive(skill.IsGaugeDisplayed);
	}

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("Restocker sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		if (PCTrainingApp.Instance != null)
		{
			foreach (RestockerSkillData data in TrainingData)
			{
				if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.m_RestockersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				RestockerSkillData data = TrainingData.FirstOrDefault((RestockerSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired restocker id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered restocker id={id} UI panel");
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<Clerk> active = employeeManager.m_ActiveRestockers;
		int activeCount = 0;
		if (active != null)
		{
			foreach (Clerk clerk in active)
			{
				if (clerk != null)
				{
					AssignClerk(clerk);
					activeCount++;
				}
			}
		}
		Plugin.LogInfo($"Restocker sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
