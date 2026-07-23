using System;
using System.Linq;
using UnityEngine;

namespace EmployeeTraining.Employee;

public abstract class EmployeeSkill<S, ST, E, T> : IEmployeeSkill where S : IEmployeeSkill where ST : ISkillTier where E : Employee<T>, new() where T : MonoBehaviour
{
	private readonly E employee = new E();

	internal readonly SkillData<S> data;

	public virtual T Employee
	{
		get
		{
			return employee.Instance;
		}
		set
		{
			employee.Instance = value;
		}
	}

	public int Id => data.Id;

	public virtual string JobName => typeof(T).Name;

	public GameObject ExpGaugeObj { get; set; }

	public GameObject TrainingStatusPanelObj { get; set; }

	public Action<int, bool> OnExpChanged { get; set; }

	public Action<bool> OnLevelChanged { get; set; }

	internal abstract ST[] SkillTable { get; }

	public int Exp => TotalExp - Tier.Exp;

	public int TotalExp
	{
		get
		{
			return data.Exp;
		}
		private set
		{
			data.Exp = value;
			UpdateStatus();
		}
	}

	public Grade Grade
	{
		get
		{
			return Grade.List[data.Grade];
		}
		set
		{
			data.Grade = value.Order;
		}
	}

	public bool IsGaugeDisplayed
	{
		get
		{
			return data.IsGaugeDisplayed;
		}
		set
		{
			data.IsGaugeDisplayed = value;
		}
	}

	internal ST Tier { get; set; }

	public abstract float Wage { get; }

	public float InitialWage { get; internal set; }

	public abstract float HiringCost { get; }

	public float InitialHiringCost { get; internal set; }

	public int Lvl => Tier.Lvl;

	protected abstract float CostRateToLevelUp { get; }

	public bool IsAssigned()
	{
		return (Object)(object)Employee != (Object)null;
	}

	protected EmployeeSkill(SkillData<S> data)
	{
		this.data = data;
		Tier = SkillTable[0];
	}

	public abstract float GetWage(Grade g);

	internal abstract void ApplyWageToGame(float dailyWage, float hiringCost);

	public abstract void Setup();

	public void AddExp(int exp)
	{
		if (!TrainingNetworkSync.CanGrantExp)
		{
			return;
		}

		TotalExp += exp;
		try
		{
			OnExpChanged?.Invoke(exp, arg2: true);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"OnExpChanged failed for {this}: {ex.Message}");
		}
		ETSaveManager.SaveCurrent();
	}

	public void UpdateStatus(bool init = false)
	{
		bool leveled = UpdateLvl();
		if (leveled || init)
		{
			Grade = Grade.List.First((Grade g) => Lvl <= g.LvlMax);
		}

		try
		{
			ApplyWageToGame(Wage, HiringCost);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"ApplyWageToGame failed for {this}: {ex.Message}");
		}

		if (init)
		{
			try
			{
				OnExpChanged?.Invoke(0, arg2: true);
			}
			catch (Exception ex)
			{
				Plugin.LogWarn($"OnExpChanged(init) failed for {this}: {ex.Message}");
			}
		}

		if (leveled && !init)
		{
			try
			{
				OnLevelChanged?.Invoke(obj: true);
			}
			catch (Exception ex)
			{
				Plugin.LogWarn($"OnLevelChanged failed for {this}: {ex.Message}");
			}
		}
	}

	private bool UpdateLvl()
	{
		int lvl = Lvl;
		if (Lvl >= Grade.LvlMax)
		{
			return false;
		}
		for (int i = lvl - 1; i < SkillTable.Length && data.Exp >= SkillTable[i].Exp; i++)
		{
			Tier = SkillTable[i];
		}
		if (Lvl > Grade.LvlMax)
		{
			Tier = SkillTable[Grade.LvlMax - 1];
		}
		return Lvl > lvl;
	}

	public void UnlockGrade()
	{
		if (!TrainingNetworkSync.CanGrantExp)
		{
			return;
		}

		Grade grade = Grade.List.FirstOrDefault((Grade g) => g.Order == Grade.Order + 1);
		if (grade != null)
		{
			Grade = grade;
			UpdateStatus();
			OnExpChanged?.Invoke(0, arg2: true);
			ApplyWageToGame(Wage, HiringCost);
			ETSaveManager.SaveCurrent();
		}
	}

	public int? GetExpForNext()
	{
		if (Tier.Lvl < SkillTable.Length)
		{
			return SkillTable[Tier.Lvl].Exp - Tier.Exp;
		}
		return null;
	}

	public int? GetTotalExpForNext()
	{
		if (Tier.Lvl < SkillTable.Length)
		{
			return SkillTable[Tier.Lvl].Exp;
		}
		return null;
	}

	public bool IsUnlockNeeded()
	{
		int? totalExpForNext = GetTotalExpForNext();
		if (totalExpForNext.HasValue)
		{
			return Lvl >= Grade.LvlMax && TotalExp >= totalExpForNext;
		}
		return false;
	}

	public float? GetCostToUpgrade()
	{
		return Grade.Cost;
	}

	public string GetExpDisplay()
	{
		int? expForNext = GetExpForNext();
		if (expForNext.HasValue)
		{
			return $"{Exp}<size=70%> / {expForNext}</size>";
		}
		return $"{TotalExp}";
	}

	public ExpRange GetExpRangeOfGrade(Grade g)
	{
		return new ExpRange
		{
			Start = SkillTable[g.LvlMin - 1].Exp,
			End = SkillTable[Math.Min(g.LvlMax, SkillTable.Length - 1)].Exp - 1
		};
	}

	public override string ToString()
	{
		return $"{typeof(T)}[{Id}] exp={TotalExp}, lvl={Lvl}, grade={Grade.Name}";
	}

	public float? GetCostToLevelup()
	{
		int? expForNext = GetExpForNext();
		if (!expForNext.HasValue || Grade.Order > Grade.Adv.Order)
		{
			return null;
		}
		return (float?)(expForNext - Exp) * CostRateToLevelUp;
	}

	public void TrainToLevelup()
	{
		int? expForNext = GetExpForNext();
		if (expForNext.HasValue)
		{
			AddExp(expForNext.Value - Exp);
		}
	}

	public void OnFired()
	{
		ExpGaugeObj = null;
		Employee = default(T);
	}

	public abstract void Despawn();
}
