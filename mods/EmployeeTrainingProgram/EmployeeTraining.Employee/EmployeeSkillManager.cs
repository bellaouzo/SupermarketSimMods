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
		D val = TrainingData.FirstOrDefault((D c) => c.Id == id);
		if (val == null)
		{
			return;
		}

		S skill = val.Skill;
		if (skill != null)
		{
			if ((Object)(object)skill.ExpGaugeObj != (Object)null)
			{
				Object.Destroy((Object)(object)skill.ExpGaugeObj);
				skill.ExpGaugeObj = null;
			}

			if (PCTrainingApp.Instance != null)
			{
				PCTrainingApp.Instance.DeleteEmployee(skill);
			}

			skill.OnFired();
		}

		TrainingData.Remove(val);
		ETSaveManager.SaveCurrent();
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
		if (id < 0)
		{
			Plugin.LogWarn($"Ignoring invalid {typeof(T).Name} training id={id}");
			return null;
		}

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
		if (id < 0)
		{
			return null;
		}
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
		if (id < 0)
		{
			Plugin.LogWarn($"Skipping {typeof(T).Name} with invalid id={id}");
			return null;
		}
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
		if (skill == null)
		{
			return null;
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
		GenerateSkillIndiactor(skill);
		if (newlyRegistered)
		{
			Plugin.LogInfo($"{typeof(T).Name}[{id}] loaded: {skill}");
		}
		return skill;
	}

	public virtual void BindWorldEmployees()
	{
		if (SkillIndicatorGenerator.SkillIndicatorTmpl == null)
		{
			return;
		}

		T[] world;
		try
		{
			world = Object.FindObjectsOfType<T>();
		}
		catch
		{
			return;
		}

		if (world == null || world.Length == 0)
		{
			return;
		}

		for (int i = 0; i < world.Length; i++)
		{
			T employee = world[i];
			if ((Object)(object)employee == (Object)null)
			{
				continue;
			}

			try
			{
				if (GetId(employee) < 0)
				{
					continue;
				}

				if ((Object)(object)((Component)(object)employee).GetComponentInChildren<SkillIndicator>(true) != (Object)null
					&& GetSkill(employee) != null)
				{
					continue;
				}

				AssignSkill(employee);
			}
			catch (Exception ex)
			{
				Plugin.LogWarn($"{typeof(T).Name} world bind failed: {ex.Message}");
			}
		}
	}

	public void DeduplicateTrainingData()
	{
		PurgeInvalidTrainingData();
		if (TrainingData.Count <= 1)
		{
			return;
		}
		Dictionary<int, D> bestById = new Dictionary<int, D>();
		List<D> duplicates = new List<D>();
		foreach (D entry in TrainingData)
		{
			if (entry == null)
			{
				continue;
			}
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
			DestroyTrainingUi(duplicate);
			TrainingData.Remove(duplicate);
		}
		Plugin.LogInfo($"Removed {duplicates.Count} duplicate {typeof(T).Name} training entries");
	}

	public void PurgeInvalidTrainingData()
	{
		List<D> invalid = null;
		foreach (D entry in TrainingData)
		{
			if (entry == null || entry.Id < 0)
			{
				invalid ??= new List<D>();
				invalid.Add(entry);
			}
		}

		if (invalid == null || invalid.Count == 0)
		{
			return;
		}

		foreach (D entry in invalid)
		{
			DestroyTrainingUi(entry);
			TrainingData.Remove(entry);
		}

		Plugin.LogInfo($"Removed {invalid.Count} invalid {typeof(T).Name} training entries (id < 0)");
	}

	private static void DestroyTrainingUi(D entry)
	{
		if (entry?.Skill == null)
		{
			return;
		}

		try
		{
			if (PCTrainingApp.Instance != null && (Object)(object)entry.Skill.TrainingStatusPanelObj != (Object)null)
			{
				PCTrainingApp.Instance.DeleteEmployee(entry.Skill);
			}
		}
		catch
		{
		}

		if ((Object)(object)entry.Skill.TrainingStatusPanelObj != (Object)null)
		{
			Object.Destroy((Object)(object)entry.Skill.TrainingStatusPanelObj);
			entry.Skill.TrainingStatusPanelObj = null;
		}
		if ((Object)(object)entry.Skill.ExpGaugeObj != (Object)null)
		{
			Object.Destroy((Object)(object)entry.Skill.ExpGaugeObj);
			entry.Skill.ExpGaugeObj = null;
		}
	}

	public IEnumerable<S> GetSkills()
	{
		return TrainingData.Select((D c) => c.Skill).AsEnumerable();
	}

	public abstract int GetId(T employee);

	public void GenerateSkillIndiactor(S skill)
	{
		if (skill?.Employee == null || SkillIndicatorGenerator.SkillIndicatorTmpl == null)
		{
			return;
		}

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
