using System;
using UnityEngine;

namespace EmployeeTraining.EmployeeIceCream;

public static class IceCreamHelperLogic
{
	public static void ApplyRapidity(IceCreamHelper helper, int? boostLevel = null)
	{
		if (helper == null)
		{
			return;
		}
		IceCreamHelperSkill skill = IceCreamHelperSkillManager.Instance.GetOrAssignSkill(helper);
		if (skill == null)
		{
			return;
		}
		int index = boostLevel ?? helper.m_CurrentBoostLevel;
		float intervalMult = Employee.BoostStacking.IntervalMultiplier(helper.m_PerActionIntervals, index);
		float animMult = Employee.BoostStacking.SpeedMultiplier(helper.m_HelperAnimationSpeeds, index);
		helper.m_CurrentActionDelayInterval = Mathf.Max(0.05f, skill.ActionInterval * intervalMult / Mathf.Max(animMult, 0.01f));
	}

	public static void GiveExp(IceCreamHelper helper, int amount, string source)
	{
		if (helper == null)
		{
			return;
		}
		try
		{
			IceCreamHelperSkill skill = IceCreamHelperSkillManager.Instance.GetSkill(helper)
				?? IceCreamHelperSkillManager.Instance.AssignHelper(helper);
			if (skill == null)
			{
				return;
			}
			skill.AddExp(amount);
			Plugin.LogInfo($"IceCreamHelper[{helper.ID}] +{amount} XP ({source}) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"IceCreamHelper XP ({source}) failed: {ex.Message}");
		}
	}
}
