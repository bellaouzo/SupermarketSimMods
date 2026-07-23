using System;
using HarmonyLib;

namespace EmployeeTraining.EmployeeSecurity;

[HarmonyPatch]
public static class SecuritySpeedPatch
{
	[HarmonyPatch(typeof(SecurityGuardAnimationController), "SetSpeed")]
	[HarmonyPriority(Priority.First)]
	[HarmonyPrefix]
	public static bool SecurityGuardAnimationController_SetSpeed_Prefix(SecurityGuardAnimationController __instance, int _speedLevel)
	{
		try
		{
			SecurityLogic.SetSpeed(__instance, _speedLevel, __instance.m_Agent);
			return false;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Security SetSpeed failed, using vanilla: {ex.Message}");
			return true;
		}
	}
}
