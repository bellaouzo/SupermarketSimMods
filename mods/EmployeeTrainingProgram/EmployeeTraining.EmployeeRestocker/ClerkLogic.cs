using System.Collections.Generic;
using EmployeeTraining.Employee;
using MyBox;
using SupermarketSimulator.Clerk;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeTraining.EmployeeRestocker;

public static class ClerkLogic
{
	private static readonly Dictionary<int, int> LastBoostLevel = new Dictionary<int, int>();

	public static void RememberBoost(Clerk clerk, int boostLevel)
	{
		if (clerk == null)
		{
			return;
		}
		LastBoostLevel[clerk.EmployeeId] = Mathf.Max(0, boostLevel);
	}

	public static int GetBoostLevel(Clerk clerk)
	{
		if (clerk == null)
		{
			return 0;
		}
		if (LastBoostLevel.TryGetValue(clerk.EmployeeId, out int level))
		{
			return level;
		}
		return 0;
	}

	public static void ApplyRapidity(Clerk clerk, int? boostLevel = null)
	{
		if (clerk == null || RestockerSkillManager.Instance == null)
		{
			return;
		}
		RestockerSkill skill = RestockerSkillManager.Instance.GetSkillById(clerk.EmployeeId)
			?? RestockerSkillManager.Instance.Register(clerk.EmployeeId);
		if (skill == null)
		{
			return;
		}
		int level = boostLevel ?? GetBoostLevel(clerk);
		RememberBoost(clerk, level);
		try
		{
			NavMeshAgent agent = clerk.m_NavmeshAgent;
			float walkMult = BoostStacking.SpeedMultiplier(clerk.m_RestockerWalkingSpeeds, level);
			if ((UnityEngine.Object)(object)agent != (UnityEngine.Object)null)
			{
				agent.speed = skill.AgentSpeed * walkMult;
				agent.angularSpeed = Mathf.Max(120f, skill.AgentAngularSpeed * walkMult);
				agent.acceleration = Mathf.Max(8f, skill.AgentAcceleration * walkMult);
			}
			float placeMult = BoostStacking.IntervalMultiplier(clerk.m_RestockerPlacingSpeeds, level);
			clerk.m_PlacingProductsInterval = Mathf.Max(0.05f, skill.ProductPlacingIntv * placeMult);
		}
		catch (System.Exception ex)
		{
			Plugin.LogWarn("Clerk ApplyRapidity failed: " + ex.Message);
		}
	}

	public static void ApplyRapidityForEmployeeId(int employeeId)
	{
		EmployeeManager manager = Singleton<EmployeeManager>.Instance;
		Il2CppSystem.Collections.Generic.List<Clerk> active = manager?.m_ActiveRestockers;
		if (active == null)
		{
			return;
		}
		foreach (Clerk clerk in active)
		{
			if (clerk != null && clerk.EmployeeId == employeeId)
			{
				ApplyRapidity(clerk);
				return;
			}
		}
	}
}
