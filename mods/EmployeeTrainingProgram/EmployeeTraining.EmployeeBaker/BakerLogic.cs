using System;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.EmployeeBaker;

public static class BakerLogic
{
	private static readonly PrivateFldStatic<NavMeshAgent> fldAgent = new PrivateFldStatic<NavMeshAgent>(typeof(Baker), "m_Agent");

	public static void ApplyRapidity(Baker baker, int? boostLevel = null)
	{
		if (baker == null)
		{
			return;
		}
		BakerSkill skill = BakerSkillManager.Instance.GetOrAssignSkill(baker);
		if (skill == null)
		{
			return;
		}
		NavMeshAgent agent = baker.m_Agent ?? fldAgent.GetValue(baker);
		int level = boostLevel ?? baker.m_CurrentBoostLevel;
		float walkMult = Employee.BoostStacking.SpeedMultiplier(baker.m_BakerWalkingSpeeds, level);
		if (agent != null)
		{
			agent.speed = skill.AgentSpeed * walkMult;
			agent.angularSpeed = Mathf.Max(120f, skill.AgentAngularSpeed * walkMult);
			agent.acceleration = Mathf.Max(8f, skill.AgentAcceleration * walkMult);
		}
		float placeMult = Employee.BoostStacking.IntervalMultiplier(baker.m_BakerPlacingSpeeds, level);
		baker.m_PlacingProductsInterval = Mathf.Max(0.12f, skill.ProductPlacingIntv * placeMult);
		BakerCollectPatch.EnsureFillOvenEnabled(baker);
	}

	public static void GiveExp(Baker baker, int amount, string source)
	{
		if (baker == null)
		{
			return;
		}
		try
		{
			BakerSkill skill = BakerSkillManager.Instance.GetSkill(baker)
				?? BakerSkillManager.Instance.AssignBaker(baker);
			if (skill == null)
			{
				return;
			}
			skill.AddExp(amount);
			Plugin.LogInfo($"Baker[{baker.BakerID}] +{amount} XP ({source}) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Baker XP ({source}) failed: {ex.Message}");
		}
	}
}
