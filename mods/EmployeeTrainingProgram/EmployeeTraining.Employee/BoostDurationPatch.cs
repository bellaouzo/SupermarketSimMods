using System;
using __Project__.Scripts.Janitor;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;
using HarmonyLib;
using SupermarketSimulator.Clerk;
using UnityEngine;

namespace EmployeeTraining.Employee;

[HarmonyPatch]
public static class BoostDurationPatch
{
	private const float MinMultiplier = 1f;

	private const float MaxMultiplier = 2f;

	[HarmonyPatch(typeof(BoostIndicator), "StartBoostCountdown")]
	[HarmonyPrefix]
	public static void BoostIndicator_StartBoostCountdown_Prefix(BoostIndicator __instance)
	{
		ApplyDurationScale(__instance);
	}

	private static void ApplyDurationScale(BoostIndicator indicator)
	{
		if ((UnityEngine.Object)(object)indicator == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			IEmployeeSkill skill = ResolveSkill(indicator.transform);
			if (skill == null)
			{
				return;
			}
			float mult = GetDurationMultiplier(skill.Lvl);
			if (mult <= 1.001f)
			{
				return;
			}
			ScaleDurations(indicator, mult);
			Plugin.LogInfo($"Boost duration x{mult:0.##} for {skill.JobName}[{skill.Id}] lvl={skill.Lvl}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Boost duration scale failed: " + ex.Message);
		}
	}

	internal static float GetDurationMultiplier(int level)
	{
		int maxLvl = 1;
		foreach (Grade grade in Grade.List)
		{
			if (grade.LvlMax > maxLvl)
			{
				maxLvl = grade.LvlMax;
			}
		}
		float t = Mathf.Clamp01((level - 1f) / Mathf.Max(1f, maxLvl - 1f));
		return Mathf.Lerp(MinMultiplier, MaxMultiplier, t);
	}

	private static void ScaleDurations(BoostIndicator indicator, float mult)
	{
		Traverse traverse = Traverse.Create(indicator);
		float timeLeft = traverse.Field("m_TimeLeft").GetValue<float>();
		if (timeLeft <= 0f)
		{
			try
			{
				timeLeft = indicator.TimeLeft;
			}
			catch
			{
				timeLeft = 0f;
			}
		}
		if (timeLeft > 0f)
		{
			float scaled = timeLeft * mult;
			traverse.Field("m_TimeLeft").SetValue(scaled);
			try
			{
				indicator.TimeLeft = scaled;
			}
			catch
			{
			}
		}
		try
		{
			float[] current = traverse.Field("CurrentBoostDurations").GetValue<float[]>();
			if (current != null)
			{
				for (int i = 0; i < current.Length; i++)
				{
					current[i] *= mult;
				}
			}
		}
		catch
		{
		}
		try
		{
			float[] baselines = traverse.Field("BoostDurations").GetValue<float[]>();
			float[] current = traverse.Field("CurrentBoostDurations").GetValue<float[]>();
			if (baselines != null && current != null && baselines.Length == current.Length)
			{
				for (int i = 0; i < current.Length; i++)
				{
					if (current[i] <= baselines[i] * 1.001f)
					{
						current[i] = baselines[i] * mult;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static IEmployeeSkill ResolveSkill(Transform transform)
	{
		if ((UnityEngine.Object)(object)transform == (UnityEngine.Object)null)
		{
			return null;
		}
		Clerk clerk = transform.GetComponentInParent<Clerk>();
		if ((UnityEngine.Object)(object)clerk != (UnityEngine.Object)null)
		{
			return RestockerSkillManager.Instance?.GetSkillById(clerk.EmployeeId)
				?? RestockerSkillManager.Instance?.Register(clerk.EmployeeId);
		}
		Cashier cashier = transform.GetComponentInParent<Cashier>();
		if ((UnityEngine.Object)(object)cashier != (UnityEngine.Object)null)
		{
			return CashierSkillManager.Instance?.GetOrAssignSkill(cashier);
		}
		Janitor janitor = transform.GetComponentInParent<Janitor>();
		if ((UnityEngine.Object)(object)janitor != (UnityEngine.Object)null)
		{
			return JanitorSkillManager.Instance?.GetOrAssignSkill(janitor);
		}
		CustomerHelper helper = transform.GetComponentInParent<CustomerHelper>();
		if ((UnityEngine.Object)(object)helper != (UnityEngine.Object)null)
		{
			return CsHelperSkillManager.Instance?.GetOrAssignSkill(helper);
		}
		Baker baker = transform.GetComponentInParent<Baker>();
		if ((UnityEngine.Object)(object)baker != (UnityEngine.Object)null)
		{
			return BakerSkillManager.Instance?.GetOrAssignSkill(baker);
		}
		IceCreamHelper ice = transform.GetComponentInParent<IceCreamHelper>();
		if ((UnityEngine.Object)(object)ice != (UnityEngine.Object)null)
		{
			return IceCreamHelperSkillManager.Instance?.GetOrAssignSkill(ice);
		}
		SecurityGuard security = transform.GetComponentInParent<SecurityGuard>();
		if ((UnityEngine.Object)(object)security != (UnityEngine.Object)null)
		{
			return SecuritySkillManager.Instance?.GetOrAssignSkill(security);
		}
		Restocker restocker = transform.GetComponentInParent<Restocker>();
		if ((UnityEngine.Object)(object)restocker != (UnityEngine.Object)null)
		{
			return RestockerSkillManager.Instance?.GetOrAssignSkill(restocker);
		}
		return null;
	}
}
