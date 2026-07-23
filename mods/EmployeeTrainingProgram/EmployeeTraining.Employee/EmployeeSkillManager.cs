using System;
using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.Employee;

public abstract class EmployeeSkillManager<S, ST, D, E, T> where S : EmployeeSkill<S, ST, E, T> where ST : ISkillTier where D : SkillData<S>, new() where E : Employee<T>, new() where T : MonoBehaviour
{
	internal abstract List<D> TrainingData { get; }

	internal EmployeeSkillManager()
	{
		Plugin instance = Plugin.Instance;
		instance.GameQuitEvent = (Action)Delegate.Combine(instance.GameQuitEvent, new Action(OnGameQuit));
	}

	public virtual void Hire(int id)
	{
	}

	public virtual void Fire(int id)
	{
		Plugin.LogDebug($"Firing {typeof(T).Name}[{id}]");
		D val = TrainingData.First((D c) => c.Id == id);
		if (val != null)
		{
			S skill = val.Skill;
			Object.Destroy((Object)(object)skill.ExpGaugeObj);
			PCTrainingApp.Instance.DeleteEmployee(skill);
			skill.OnFired();
		}
	}

	public virtual T Spawn(Il2CppSystem.Collections.Generic.List<T> employees, int employeeID)
	{
		Plugin.LogDebug($"Spawned {typeof(T).Name}[{employeeID}]");
		T val = default(T);
		for (int i = employees.Count - 1; i >= 0; i--)
		{
			T candidate = employees[i];
			if (GetId(candidate) == employeeID)
			{
				val = candidate;
				break;
			}
		}
		S orAssignSkill = GetOrAssignSkill(val);
		GenerateSkillIndiactor(orAssignSkill);
		return val;
	}

	public virtual T Spawn(List<T> employees, int employeeID)
	{
		var il2CppList = new Il2CppSystem.Collections.Generic.List<T>(employees.Count);
		foreach (T employee in employees)
		{
			il2CppList.Add(employee);
		}
		return Spawn(il2CppList, employeeID);
	}

	public virtual void Despawn(T employee)
	{
		Plugin.LogDebug($"Despawned {typeof(T).Name}[{GetId(employee)}]");
		GetSkill(employee)?.Despawn();
	}

	public virtual void OnGameQuit()
	{
		foreach (D entry in TrainingData)
		{
			entry?.Skill?.Despawn();
		}
		Plugin.LogInfo("Detached live employees for " + GetType().Name + " (training levels kept in memory/disk).");
	}

	public virtual S Register(int id)
	{
		D existing = TrainingData.FirstOrDefault((D c) => c.Id == id);
		if (existing != null)
		{
			if ((Object)(object)existing.Skill.TrainingStatusPanelObj == (Object)null && PCTrainingApp.Instance != null)
			{
				PCTrainingApp.Instance.RegisterEmployee(existing.Skill);
			}
			return existing.Skill;
		}
		D val = new D
		{
			Id = id
		};
		TrainingData.Add(val);
		if (PCTrainingApp.Instance != null)
		{
			PCTrainingApp.Instance.RegisterEmployee(val.Skill);
		}
		return val.Skill;
	}

	public S GetSkill(T employee)
	{
		if ((Object)(object)employee == (Object)null)
		{
			return null;
		}
		int id = GetId(employee);
		D data = TrainingData.FirstOrDefault((D c) => c.Id == id);
		return data != null ? data.Skill : null;
	}

	public S GetOrAssignSkill(T employee)
	{
		return AssignSkill(employee);
	}

	public S AssignSkill(T employee)
	{
		if ((Object)(object)employee == (Object)null)
		{
			return null;
		}
		int id = GetId(employee);
		D data = TrainingData.FirstOrDefault((D c) => c.Id == id);
		S skill;
		bool newlyRegistered = false;
		if (data == null)
		{
			skill = Register(id);
			newlyRegistered = true;
		}
		else
		{
			skill = data.Skill;
			if ((Object)(object)skill.TrainingStatusPanelObj == (Object)null && PCTrainingApp.Instance != null)
			{
				PCTrainingApp.Instance.RegisterEmployee(skill);
			}
		}
		skill.Employee = employee;
		try
		{
			skill.UpdateStatus(init: true);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"{typeof(T).Name}[{id}] level sync failed: {ex.Message}");
		}
		if (newlyRegistered)
		{
			Plugin.LogInfo($"{typeof(T).Name}[{id}] loaded: {skill}");
		}
		return skill;
	}

	public void DeduplicateTrainingData()
	{
		if (TrainingData.Count <= 1)
		{
			return;
		}
		Dictionary<int, D> bestById = new Dictionary<int, D>();
		List<D> duplicates = new List<D>();
		foreach (D entry in TrainingData)
		{
			if (!bestById.TryGetValue(entry.Id, out D existing))
			{
				bestById[entry.Id] = entry;
				continue;
			}
			if (entry.Skill.TotalExp > existing.Skill.TotalExp)
			{
				duplicates.Add(existing);
				bestById[entry.Id] = entry;
			}
			else
			{
				duplicates.Add(entry);
			}
		}
		if (duplicates.Count == 0)
		{
			return;
		}
		foreach (D duplicate in duplicates)
		{
			if ((Object)(object)duplicate.Skill.TrainingStatusPanelObj != (Object)null)
			{
				Object.Destroy((Object)(object)duplicate.Skill.TrainingStatusPanelObj);
				duplicate.Skill.TrainingStatusPanelObj = null;
			}
			if ((Object)(object)duplicate.Skill.ExpGaugeObj != (Object)null)
			{
				Object.Destroy((Object)(object)duplicate.Skill.ExpGaugeObj);
				duplicate.Skill.ExpGaugeObj = null;
			}
			TrainingData.Remove(duplicate);
		}
		Plugin.LogInfo($"Removed {duplicates.Count} duplicate {typeof(T).Name} training entries");
	}

	public IEnumerable<S> GetSkills()
	{
		return TrainingData.Select((D c) => c.Skill).AsEnumerable();
	}

	public abstract int GetId(T employee);

	public void GenerateSkillIndiactor(S skill)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		T employee = skill.Employee;
		if ((Object)(object)((Component)(object)employee).GetComponentInChildren<SkillIndicator>() == (Object)null)
		{
			Plugin.LogDebug($"Adding Skill Indicator for {typeof(T)} {GetId(employee)}");
			GameObject val = Object.Instantiate<GameObject>(SkillIndicatorGenerator.SkillIndicatorTmpl, ((Component)(object)employee).transform, false);
			SkillIndicator component = val.GetComponent<SkillIndicator>();
			((Component)component).transform.localPosition = new Vector3(0.18f, Plugin.Instance.Settings.GaugeHeight, 0f);
			((Component)component).gameObject.SetActive(true);
			component.SetUp(skill, Plugin.Localizer);
			skill.ExpGaugeObj = val;
			val.SetActive(skill.IsGaugeDisplayed);
		}
	}
}
