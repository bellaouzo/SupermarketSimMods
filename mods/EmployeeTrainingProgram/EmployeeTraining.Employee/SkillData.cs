using System;

namespace EmployeeTraining.Employee;

[Serializable]
public abstract class SkillData<S> where S : IEmployeeSkill
{
	public int Id;

	public int Exp = 0;

	public int Grade = 0;

	public bool IsGaugeDisplayed = true;

	[NonSerialized]
	public S Skill;

	public SkillData()
	{
		if (ETSaveManager.SuppressSkillDataLoadSubscription)
		{
			return;
		}

		ETSaveManager.SaveDataLoadedEvent = (Action)Delegate.Combine(ETSaveManager.SaveDataLoadedEvent, new Action(OnLoad));
	}

	public void SyncFromSave()
	{
		if (Skill == null)
		{
			return;
		}

		try
		{
			Skill.Setup();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Skill setup on load failed (id={Id}): {ex.Message}");
			try
			{
				Skill.UpdateStatus(init: true);
			}
			catch (Exception syncEx)
			{
				Plugin.LogWarn($"Skill level sync on load failed (id={Id}): {syncEx.Message}");
			}
		}
	}

	private void OnLoad()
	{
		try
		{
			SyncFromSave();
		}
		finally
		{
			ETSaveManager.SaveDataLoadedEvent = (Action)Delegate.Remove(ETSaveManager.SaveDataLoadedEvent, new Action(OnLoad));
		}
	}
}
