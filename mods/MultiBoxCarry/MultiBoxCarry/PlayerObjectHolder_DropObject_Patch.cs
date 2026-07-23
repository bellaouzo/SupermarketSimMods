using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerObjectHolder), "DropObject")]
internal static class PlayerObjectHolder_DropObject_Patch
{
	[HarmonyPostfix]
	private static void Postfix(PlayerObjectHolder __instance)
	{
		try
		{
			if (!((Object)(object)__instance == (Object)null))
			{
				__instance.SetNullCurrentObject();
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[PlayerObjectHolder_DropObject_Patch] " + ex));
		}
	}
}
