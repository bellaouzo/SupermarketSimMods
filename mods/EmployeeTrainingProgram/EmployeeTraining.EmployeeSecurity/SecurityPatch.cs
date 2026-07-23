using HarmonyLib;
using Il2CppList = Il2CppSystem.Collections.Generic.List<SecurityGuard>;

namespace EmployeeTraining.EmployeeSecurity;

[HarmonyPatch]
public static class SecurityPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "SpawnSecurityGuard")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnSecurityGuard_Postfix(EmployeeManager __instance, int securityGuardID)
	{
		Il2CppList active = __instance.m_ActiveSecurityGuards;
		if (active != null)
		{
			SecuritySkillManager.Instance.Spawn(active, securityGuardID);
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireSecurityGuard")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireSecurityGuard_Postfix(EmployeeManager __instance, int SecurityGuardID)
	{
		SecuritySkillManager.Instance.Fire(SecurityGuardID);
	}

	[HarmonyPatch(typeof(EmployeeManager), "DespawnSecurityGuard")]
	[HarmonyPrefix]
	public static void CheckoutManager_DespawnSecurityGuard_Prefix(EmployeeManager __instance, int SecurityGuardID)
	{
		Il2CppList active = __instance.m_ActiveSecurityGuards;
		if (active == null)
		{
			return;
		}
		SecurityGuard match = null;
		foreach (SecurityGuard guard in active)
		{
			if (guard != null && guard.ID == SecurityGuardID)
			{
				match = guard;
				break;
			}
		}
		if (match != null)
		{
			SecuritySkillManager.Instance.Despawn(match);
		}
	}
}
