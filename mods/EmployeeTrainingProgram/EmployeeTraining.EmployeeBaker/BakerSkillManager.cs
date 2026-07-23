using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

public class BakerSkillManager : EmployeeSkillManager<BakerSkill, BakerSkillTier, BakerSkillData, EmplBaker, Baker>
{
	public static BakerSkillManager Instance;

	internal override List<BakerSkillData> TrainingData => ETSaveManager.Data.BakerSkills;

	static BakerSkillManager()
	{
		Instance = new BakerSkillManager();
	}

	public override int GetId(Baker employee)
	{
		return employee.BakerID;
	}

	public BakerSkill AssignBaker(Baker baker)
	{
		if (baker == null)
		{
			return null;
		}
		BakerSkill skill = GetOrAssignSkill(baker);
		if (skill != null && SkillIndicatorGenerator.SkillIndicatorTmpl != null && (Object)(object)skill.ExpGaugeObj == (Object)null)
		{
			GenerateSkillIndiactor(skill);
		}
		return skill;
	}

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("Baker sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.m_BakersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				BakerSkillData data = TrainingData.FirstOrDefault((BakerSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired baker id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered baker id={id} UI panel");
				}
			}
		}
		int activeCount = 0;
		BakeryManager bakery = BakeryManager.Instance;
		Il2CppSystem.Collections.Generic.List<Baker> active = bakery?.Bakers;
		if (active != null)
		{
			foreach (Baker baker in active)
			{
				if (baker != null)
				{
					AssignBaker(baker);
					BakerLogic.ApplyRapidity(baker);
					activeCount++;
				}
			}
		}
		Plugin.LogInfo($"Baker sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
