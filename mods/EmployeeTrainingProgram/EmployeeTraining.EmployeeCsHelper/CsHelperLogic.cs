using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using MyBox;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.EmployeeCsHelper;

public static class CsHelperLogic
{
	private static readonly Dictionary<int, float[]> VanillaScanBaselines = new Dictionary<int, float[]>();

	public static void Init()
	{
	}

	public static void ApplyRapidity(CustomerHelper cshelper, int? boostLevel = null)
	{
		if (cshelper == null)
		{
			return;
		}
		CsHelperSkill skill = CsHelperSkillManager.Instance.GetOrAssignSkill(cshelper);
		NavMeshAgent agent = cshelper.m_Agent;
		if (skill == null || agent == null)
		{
			return;
		}
		int level = boostLevel ?? cshelper.m_CurrentBoostLevel;
		float walkMult = Employee.BoostStacking.SpeedMultiplier(cshelper.m_CustomerHelperWalkingSpeeds, level);
		agent.speed = skill.AgentSpeed * walkMult;
		agent.angularSpeed = Mathf.Max(120f, skill.AgentAngularSpeed * walkMult);
		agent.acceleration = Mathf.Max(8f, skill.AgentAcceleration * walkMult);
		Il2CppSystem.Collections.Generic.List<float> scanIntervals = cshelper.m_CustomerHelperScanIntervals;
		if (scanIntervals != null && scanIntervals.Count > 0)
		{
			int key = RuntimeHelpers.GetHashCode(cshelper);
			if (!VanillaScanBaselines.TryGetValue(key, out float[] baseline) || baseline == null || baseline.Length != scanIntervals.Count)
			{
				baseline = new float[scanIntervals.Count];
				for (int i = 0; i < scanIntervals.Count; i++)
				{
					baseline[i] = scanIntervals[i];
				}
				VanillaScanBaselines[key] = baseline;
			}
			float bootMult = 1f;
			int idx = Employee.BoostStacking.ClampIndex(level, baseline.Length);
			if (baseline[0] > 0.01f)
			{
				bootMult = Mathf.Max(baseline[idx] / baseline[0], 0.05f);
			}
			float trained = (skill.IntervalMin + skill.IntervalMax) * 0.5f * bootMult;
			scanIntervals[idx] = Mathf.Max(0.05f, trained);
		}
	}

	public static void WatchHelpAndGrantExp(SelfCheckout checkout, CustomerHelper helper)
	{
		if (checkout == null || helper == null)
		{
			return;
		}
		checkout.StartCoroutine(Watch().WrapToIl2Cpp());
		IEnumerator Watch()
		{
			float timeout = 45f;
			float elapsed = 0f;
			bool sawHelping = false;
			while (elapsed < timeout && helper != null)
			{
				if (helper.isHelping || checkout.IsScanning)
				{
					sawHelping = true;
					break;
				}
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (!sawHelping || helper == null)
			{
				yield break;
			}
			while (helper != null && (helper.isHelping || checkout.IsScanning) && elapsed < timeout)
			{
				elapsed += Time.deltaTime;
				yield return null;
			}
			if (helper == null)
			{
				yield break;
			}
			CsHelperSkill skill = CsHelperSkillManager.Instance.GetSkill(helper)
				?? CsHelperSkillManager.Instance.GetOrAssignSkill(helper);
			if (skill == null)
			{
				yield break;
			}
			skill.AddExp(3);
			Plugin.LogInfo($"CustomerHelper[{helper.CustomerHelperID}] +3 XP (help-finished) totalExp={skill.TotalExp}");
		}
	}
}
