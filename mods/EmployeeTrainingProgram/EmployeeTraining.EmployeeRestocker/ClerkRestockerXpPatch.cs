using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SupermarketSimulator.Clerk;
using SupermarketSimulator.Clerk.States;
using UnityEngine;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch]
public static class ClerkRestockerXpPatch
{
	private static readonly Dictionary<int, Clerk> RestockOwners = new Dictionary<int, Clerk>();

	private static readonly Dictionary<string, float> LastXpAt = new Dictionary<string, float>();

	[HarmonyPatch(typeof(RestockDisplay), "OnBegin")]
	[HarmonyPrefix]
	public static void RestockDisplay_OnBegin_Prefix(RestockDisplay __instance)
	{
		CaptureOwner(__instance);
	}

	[HarmonyPatch(typeof(RestockDisplay), nameof(RestockDisplay.OnEnd))]
	[HarmonyPrefix]
	public static void RestockDisplay_OnEnd_Prefix(RestockDisplay __instance)
	{
		CaptureOwner(__instance);
	}

	[HarmonyPatch(typeof(RestockDisplay), nameof(RestockDisplay.OnEnd))]
	[HarmonyPostfix]
	public static void RestockDisplay_OnEnd_Postfix(RestockDisplay __instance)
	{
		GiveXp(ResolveOwner(__instance), 2, "stock-display");
		ForgetOwner(__instance);
	}

	[HarmonyPatch(typeof(Clerk), nameof(Clerk.PlaceProduct_Network))]
	[HarmonyPostfix]
	public static void Clerk_PlaceProduct_Network_Postfix(Clerk __instance)
	{
		GiveXp(__instance, 1, "place-product");
	}

	[HarmonyPatch(typeof(Clerk), nameof(Clerk.PlaceBox_Network))]
	[HarmonyPostfix]
	public static void Clerk_PlaceBox_Network_Postfix(Clerk __instance)
	{
		GiveXp(__instance, 1, "place-box");
	}

	[HarmonyPatch(typeof(NetworkClerk), nameof(NetworkClerk.PlaceProducts_Broadcast))]
	[HarmonyPostfix]
	public static void NetworkClerk_PlaceProducts_Broadcast_Postfix(NetworkClerk __instance)
	{
		GiveXp(GetClerk(__instance), 1, "place-product-broadcast");
	}

	[HarmonyPatch(typeof(NetworkClerk), nameof(NetworkClerk.ClerkPlaceProduct_RPC))]
	[HarmonyPostfix]
	public static void NetworkClerk_ClerkPlaceProduct_RPC_Postfix(NetworkClerk __instance)
	{
		GiveXp(GetClerk(__instance), 1, "place-product-rpc");
	}

	[HarmonyPatch(typeof(NetworkClerk), nameof(NetworkClerk.PlaceBox_Broadcast))]
	[HarmonyPostfix]
	public static void NetworkClerk_PlaceBox_Broadcast_Postfix(NetworkClerk __instance)
	{
		GiveXp(GetClerk(__instance), 1, "place-box-broadcast");
	}

	[HarmonyPatch(typeof(NetworkClerk), nameof(NetworkClerk.ClerkPlaceBox_RPC))]
	[HarmonyPostfix]
	public static void NetworkClerk_ClerkPlaceBox_RPC_Postfix(NetworkClerk __instance)
	{
		GiveXp(GetClerk(__instance), 1, "place-box-rpc");
	}

	[HarmonyPatch(typeof(ClerkRestockingState), nameof(ClerkRestockingState.Exit))]
	[HarmonyPostfix]
	public static void ClerkRestockingState_Exit_Postfix(ClerkRestockingState __instance)
	{
		if (__instance == null)
		{
			return;
		}
		Clerk clerk = null;
		try
		{
			clerk = __instance.m_Clerk;
		}
		catch
		{
			clerk = null;
		}
		GiveXp(clerk, 2, "restock-state-exit");
	}

	private static void CaptureOwner(RestockDisplay instance)
	{
		Clerk clerk = GetDisplayClerk(instance);
		if (clerk != null)
		{
			RestockOwners[RuntimeHelpers.GetHashCode(instance)] = clerk;
		}
	}

	private static Clerk ResolveOwner(RestockDisplay instance)
	{
		Clerk clerk = GetDisplayClerk(instance);
		if (clerk != null)
		{
			return clerk;
		}
		RestockOwners.TryGetValue(RuntimeHelpers.GetHashCode(instance), out clerk);
		return clerk;
	}

	private static Clerk GetDisplayClerk(RestockDisplay instance)
	{
		if (instance == null)
		{
			return null;
		}
		try
		{
			return instance.m_Clerk;
		}
		catch
		{
			return null;
		}
	}

	private static void ForgetOwner(RestockDisplay instance)
	{
		if (instance != null)
		{
			RestockOwners.Remove(RuntimeHelpers.GetHashCode(instance));
		}
	}

	private static Clerk GetClerk(NetworkClerk networkClerk)
	{
		if (networkClerk == null)
		{
			return null;
		}
		try
		{
			return networkClerk.m_Clerk;
		}
		catch
		{
			return null;
		}
	}

	private static void GiveXp(Clerk clerk, int amount, string source)
	{
		if (clerk == null)
		{
			Plugin.LogInfo($"Restocker XP skipped ({source}): clerk null");
			return;
		}
		try
		{
			int id = clerk.EmployeeId;
			string key = id + "|" + source;
			float now = Time.unscaledTime;
			float debounce = source.StartsWith("place-product") ? 0.12f : 0.35f;
			if (LastXpAt.TryGetValue(key, out float previous) && now - previous < debounce)
			{
				return;
			}
			LastXpAt[key] = now;
			if (RestockerSkillManager.Instance == null)
			{
				Plugin.LogWarn($"Restocker XP skipped ({source}): skill manager null");
				return;
			}
			RestockerSkill skill = RestockerSkillManager.Instance.AssignClerk(clerk);
			if (skill == null)
			{
				Plugin.LogWarn($"Restocker XP skipped ({source}): AssignClerk returned null for id={id}");
				return;
			}
			skill.AddExp(amount);
			Plugin.LogInfo($"Restocker[{id}] +{amount} XP ({source}) totalExp={skill.TotalExp}");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Restocker XP ({source}) failed: {ex.Message}");
		}
	}
}
