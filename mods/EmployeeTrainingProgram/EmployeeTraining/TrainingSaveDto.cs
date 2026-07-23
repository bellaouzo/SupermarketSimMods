using System;
using System.Collections.Generic;
using System.Linq;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;

namespace EmployeeTraining;

[Serializable]
public class TrainingSaveDto
{
	public int Version = 3;

	public SkillSaveEntry[] Cashiers = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] Restockers = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] CsHelpers = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] Janitors = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] Security = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] Bakers = Array.Empty<SkillSaveEntry>();

	public SkillSaveEntry[] IceCreamHelpers = Array.Empty<SkillSaveEntry>();

	public static TrainingSaveDto From(TrainingData data)
	{
		TrainingSaveDto dto = new TrainingSaveDto();
		if (data == null)
		{
			return dto;
		}
		dto.Cashiers = data.CashierSkills.Select(ToEntry).ToArray();
		dto.Restockers = data.RestockerSkills.Select(ToEntry).ToArray();
		dto.CsHelpers = data.CsHelperSkills.Select(ToEntry).ToArray();
		dto.Janitors = data.JanitorSkills.Select(ToEntry).ToArray();
		dto.Security = data.SecuritySkills.Select(ToEntry).ToArray();
		dto.Bakers = data.BakerSkills.Select(ToEntry).ToArray();
		dto.IceCreamHelpers = data.IceCreamHelperSkills.Select(ToEntry).ToArray();
		return dto;
	}

	public TrainingData ToTrainingData()
	{
		ETSaveManager.SuppressSkillDataLoadSubscription = true;
		try
		{
			return new TrainingData
			{
				CashierSkills = FromEntries(Cashiers, () => new CashierSkillData()),
				RestockerSkills = FromEntries(Restockers, () => new RestockerSkillData()),
				CsHelperSkills = FromEntries(CsHelpers, () => new CsHelperSkillData()),
				JanitorSkills = FromEntries(Janitors, () => new JanitorSkillData()),
				SecuritySkills = FromEntries(Security, () => new SecuritySkillData()),
				BakerSkills = FromEntries(Bakers, () => new BakerSkillData()),
				IceCreamHelperSkills = FromEntries(IceCreamHelpers, () => new IceCreamHelperSkillData())
			};
		}
		finally
		{
			ETSaveManager.SuppressSkillDataLoadSubscription = false;
		}
	}

	private static List<T> FromEntries<T>(SkillSaveEntry[] entries, Func<T> create) where T : class
	{
		List<T> list = new List<T>();
		if (entries == null)
		{
			return list;
		}
		foreach (SkillSaveEntry e in entries)
		{
			if (e == null || e.Id < 0)
			{
				continue;
			}
			T data = create();
			Apply(data, e);
			SyncLoadedSkill(data);
			list.Add(data);
		}
		return list;
	}

	private static void SyncLoadedSkill(object data)
	{
		switch (data)
		{
			case CashierSkillData cashier:
				cashier.SyncFromSave();
				break;
			case RestockerSkillData restocker:
				restocker.SyncFromSave();
				break;
			case CsHelperSkillData cs:
				cs.SyncFromSave();
				break;
			case JanitorSkillData janitor:
				janitor.SyncFromSave();
				break;
			case SecuritySkillData security:
				security.SyncFromSave();
				break;
			case BakerSkillData baker:
				baker.SyncFromSave();
				break;
			case IceCreamHelperSkillData ice:
				ice.SyncFromSave();
				break;
		}
	}

	private static SkillSaveEntry ToEntry(CashierSkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(RestockerSkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(CsHelperSkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(JanitorSkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(SecuritySkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(BakerSkillData e) => Entry(e);

	private static SkillSaveEntry ToEntry(IceCreamHelperSkillData e) => Entry(e);

	private static SkillSaveEntry Entry(CashierSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(RestockerSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(CsHelperSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(JanitorSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(SecuritySkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(BakerSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static SkillSaveEntry Entry(IceCreamHelperSkillData e)
	{
		return e == null ? new SkillSaveEntry() : new SkillSaveEntry { Id = e.Id, Exp = e.Exp, Grade = e.Grade, IsGaugeDisplayed = e.IsGaugeDisplayed };
	}

	private static void Apply(object data, SkillSaveEntry e)
	{
		if (e == null || data == null)
		{
			return;
		}
		switch (data)
		{
			case CashierSkillData cashier:
				cashier.Id = e.Id;
				cashier.Exp = e.Exp;
				cashier.Grade = e.Grade;
				cashier.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case RestockerSkillData restocker:
				restocker.Id = e.Id;
				restocker.Exp = e.Exp;
				restocker.Grade = e.Grade;
				restocker.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case CsHelperSkillData cs:
				cs.Id = e.Id;
				cs.Exp = e.Exp;
				cs.Grade = e.Grade;
				cs.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case JanitorSkillData janitor:
				janitor.Id = e.Id;
				janitor.Exp = e.Exp;
				janitor.Grade = e.Grade;
				janitor.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case SecuritySkillData security:
				security.Id = e.Id;
				security.Exp = e.Exp;
				security.Grade = e.Grade;
				security.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case BakerSkillData baker:
				baker.Id = e.Id;
				baker.Exp = e.Exp;
				baker.Grade = e.Grade;
				baker.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
			case IceCreamHelperSkillData ice:
				ice.Id = e.Id;
				ice.Exp = e.Exp;
				ice.Grade = e.Grade;
				ice.IsGaugeDisplayed = e.IsGaugeDisplayed;
				break;
		}
	}
}

[Serializable]
public class SkillSaveEntry
{
	public int Id;

	public int Exp;

	public int Grade;

	public bool IsGaugeDisplayed = true;
}
