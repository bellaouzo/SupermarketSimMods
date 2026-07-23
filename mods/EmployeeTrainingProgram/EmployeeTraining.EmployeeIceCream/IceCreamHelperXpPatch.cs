using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EmployeeTraining.EmployeeIceCream;

[HarmonyPatch]
public static class IceCreamHelperXpPatch
{
	private static readonly Dictionary<int, float> lastXpAt = new Dictionary<int, float>();

	private static readonly Dictionary<int, IceCreamHelper> pendingByCustomer = new Dictionary<int, IceCreamHelper>();

	[HarmonyPatch(typeof(Customer), nameof(Customer.DeliverIceCream))]
	[HarmonyPrefix]
	public static void Customer_DeliverIceCream_Prefix(Customer __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			IceCreamStand stand = __instance.TargetIceCreamStand ?? __instance.m_TargetIceCreamStand;
			if (stand == null)
			{
				return;
			}
			IceCreamHelper helper = stand.Employee ?? stand.m_Employee;
			if (helper == null)
			{
				return;
			}
			pendingByCustomer[__instance.GetInstanceID()] = helper;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"IceCreamHelper deliver capture failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Customer), nameof(Customer.DeliverIceCream))]
	[HarmonyPostfix]
	public static void Customer_DeliverIceCream_Postfix(Customer __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			int customerId = __instance.GetInstanceID();
			if (!pendingByCustomer.TryGetValue(customerId, out IceCreamHelper helper))
			{
				return;
			}
			pendingByCustomer.Remove(customerId);
			if (helper == null)
			{
				return;
			}
			int id = helper.ID;
			float now = Time.unscaledTime;
			if (lastXpAt.TryGetValue(id, out float previous) && now - previous < 0.5f)
			{
				return;
			}
			lastXpAt[id] = now;
			IceCreamHelperLogic.GiveExp(helper, 1, "deliver");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"IceCreamHelper deliver XP failed: {ex.Message}");
		}
	}
}
