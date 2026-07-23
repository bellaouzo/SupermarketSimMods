using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;
using Il2CppList = Il2CppSystem.Collections.Generic.List<Cashier>;

namespace EmployeeTraining.EmployeeCashier;

public class CashierSkillManager : EmployeeSkillManager<CashierSkill, CashierSkillTier, CashierSkillData, EmplCashier, Cashier>
{
	public static CashierSkillManager Instance;

	internal override List<CashierSkillData> TrainingData => ETSaveManager.Data.CashierSkills;

	static CashierSkillManager()
	{
		Instance = new CashierSkillManager();
	}

	public override int GetId(Cashier employee)
	{
		return employee.CashierID;
	}

	public void SyncExisting()
	{
		EmployeeManager employeeManager = Singleton<EmployeeManager>.Instance;
		if (employeeManager == null)
		{
			Plugin.LogWarn("Cashier sync skipped: EmployeeManager missing");
			return;
		}
		DeduplicateTrainingData();
		RegisterMissingTrainingPanels();
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.CashiersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
				if (id < 0)
				{
					continue;
				}
				CashierSkillData data = TrainingData.FirstOrDefault((CashierSkillData c) => c.Id == id);
				if (data == null)
				{
					Register(id);
					Plugin.LogInfo($"Synced hired cashier id={id} into Training App");
				}
				else if (data.Skill.TrainingStatusPanelObj == null && PCTrainingApp.Instance != null)
				{
					PCTrainingApp.Instance.RegisterEmployee(data.Skill);
					Plugin.LogInfo($"Re-registered cashier id={id} UI panel");
				}
			}
		}
		Il2CppList active = employeeManager.ActiveCashiers ?? employeeManager.m_ActiveCashiers;
		int activeCount = 0;
		if (active != null)
		{
			foreach (Cashier cashier in active)
			{
				if (cashier != null && cashier.CashierID >= 0)
				{
					Spawn(active, cashier.CashierID);
					activeCount++;
				}
			}
		}
		Plugin.LogDebug($"Cashier sync complete. hired={hired?.Count ?? 0}, active={activeCount}, training={TrainingData.Count}");
	}

	private void RegisterMissingTrainingPanels()
	{
		if (PCTrainingApp.Instance == null)
		{
			return;
		}

		foreach (CashierSkillData data in TrainingData)
		{
			if (data?.Skill != null && (Object)(object)data.Skill.TrainingStatusPanelObj == (Object)null)
			{
				PCTrainingApp.Instance.RegisterEmployee(data.Skill);
			}
		}
	}
}
