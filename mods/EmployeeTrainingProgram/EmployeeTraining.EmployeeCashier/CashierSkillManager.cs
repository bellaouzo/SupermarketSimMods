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
		Il2CppSystem.Collections.Generic.List<int> hired = employeeManager.CashiersData;
		if (hired != null)
		{
			foreach (int id in hired)
			{
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
		if (active == null)
		{
			return;
		}
		foreach (Cashier cashier in active)
		{
			if (cashier != null)
			{
				Spawn(active, cashier.CashierID);
			}
		}
		Plugin.LogInfo($"Cashier sync complete. hired={hired?.Count ?? 0}, active={active.Count}, training={TrainingData.Count}");
	}
}
