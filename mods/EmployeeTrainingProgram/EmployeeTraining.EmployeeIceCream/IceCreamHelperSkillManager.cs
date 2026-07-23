using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeIceCream;

public class IceCreamHelperSkillManager : EmployeeSkillManager<IceCreamHelperSkill, IceCreamHelperSkillTier, IceCreamHelperSkillData, EmplIceCreamHelper, IceCreamHelper>
{
	public static IceCreamHelperSkillManager Instance;

	internal override List<IceCreamHelperSkillData> TrainingData => ETSaveManager.Data.IceCreamHelperSkills;

	static IceCreamHelperSkillManager()
	{
		Instance = new IceCreamHelperSkillManager();
	}

	public override int GetId(IceCreamHelper employee)
	{
		return employee.ID;
	}

	public IceCreamHelperSkill AssignHelper(IceCreamHelper helper)
	{
		if (helper == null)
		{
			return null;
		}
		IceCreamHelperSkill skill = GetOrAssignSkill(helper);
		if (skill != null && SkillIndicatorGenerator.SkillIndicatorTmpl != null && (Object)(object)skill.ExpGaugeObj == (Object)null)
		{
			GenerateSkillIndiactor(skill);
		}
		return skill;
	}

	public override IceCreamHelper Spawn(Il2CppSystem.Collections.Generic.List<IceCreamHelper> employees, int employeeID)
	{
		IceCreamHelper helper = base.Spawn(employees, employeeID);
		IceCreamHelperLogic.ApplyRapidity(helper);
		return helper;
	}

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("IceCreamHelper sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		if (PCTrainingApp.Instance != null)
		{
			foreach (IceCreamHelperSkillData data in TrainingData)
			{
				if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.IceCreamHelpersData ?? employeeManager.m_IceCreamHelpersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				IceCreamHelperSkillData data = TrainingData.FirstOrDefault((IceCreamHelperSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired ice cream helper id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered ice cream helper id={id} UI panel");
				}
			}
		}
		Il2CppSystem.Collections.Generic.List<IceCreamHelper> active = employeeManager.m_ActiveIceCreamHelpers;
		int activeCount = 0;
		if (active != null)
		{
			foreach (IceCreamHelper helper in active)
			{
				if (helper != null)
				{
					AssignHelper(helper);
					IceCreamHelperLogic.ApplyRapidity(helper);
					activeCount++;
				}
			}
		}
		Plugin.LogInfo($"IceCreamHelper sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}
}
