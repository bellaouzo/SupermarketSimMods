using System;
using System.Collections.Generic;
using __Project__.Scripts.Multiplayer;
using HarmonyLib;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

[HarmonyPatch]
public static class BakerXpPatch
{
	private static readonly Dictionary<string, float> lastXpAt = new Dictionary<string, float>();

	private static bool TryGive(Baker baker, int amount, string source)
	{
		if (baker == null)
		{
			return false;
		}
		int id = baker.BakerID;
		string key = $"{id}:{source}";
		float now = Time.unscaledTime;
		if (lastXpAt.TryGetValue(key, out float previous) && now - previous < 0.35f)
		{
			return false;
		}
		lastXpAt[key] = now;
		BakerLogic.GiveExp(baker, amount, source);
		return true;
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.TakeProducctFromOven))]
	[HarmonyPostfix]
	public static void Baker_TakeProducctFromOven_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "take-from-oven");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.TakeProductFromOven_Network))]
	[HarmonyPostfix]
	public static void Baker_TakeProductFromOven_Network_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "take-from-oven-net");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PlaceProductToDisplay_Network))]
	[HarmonyPostfix]
	public static void Baker_PlaceProductToDisplay_Network_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "place-to-display");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PlaceProduct_Network))]
	[HarmonyPostfix]
	public static void Baker_PlaceProduct_Network_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "place-product");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PickUpBox), new Type[] { typeof(Box) })]
	[HarmonyPostfix]
	public static void Baker_PickUpBox_Box_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "pickup-box");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PickUpBox), new Type[] { typeof(RackSlot) })]
	[HarmonyPostfix]
	public static void Baker_PickUpBox_RackSlot_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "pickup-box-rack");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PickUpBox_Network))]
	[HarmonyPostfix]
	public static void Baker_PickUpBox_Network_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "pickup-box-net");
	}

	[HarmonyPatch(typeof(Baker), nameof(Baker.PlaceBoxToRack_Network))]
	[HarmonyPostfix]
	public static void Baker_PlaceBoxToRack_Network_Postfix(Baker __instance)
	{
		TryGive(__instance, 1, "place-box-rack");
	}

	[HarmonyPatch(typeof(NetworkBaker), "PlaceBoxToRack_Broadcast")]
	[HarmonyPostfix]
	public static void NetworkBaker_PlaceBoxToRack_Broadcast_Postfix(NetworkBaker __instance)
	{
		TryGive(__instance?.m_Baker, 1, "place-box-rack-broadcast");
	}
}
