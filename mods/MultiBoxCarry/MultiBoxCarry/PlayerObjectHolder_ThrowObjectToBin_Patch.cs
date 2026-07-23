using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerObjectHolder), "ThrowObjectToBin")]
internal static class PlayerObjectHolder_ThrowObjectToBin_Patch
{
	[HarmonyPostfix]
	private static void Postfix(PlayerObjectHolder __instance)
	{
		try
		{
			if (!((Object)(object)__instance == (Object)null))
			{
				if (OnThrowMessanger.OpenBoxBlockDepth > 0)
				{
					Plugin.Log.LogInfo((object)"[MultiBox] Suppressed hand clear during box open.");
					OnThrowMessanger.GaveMessage("box");
				}
				else
				{
					__instance.SetNullCurrentObject();
				}
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[PlayerObjectHolder_ThrowObjectToBin_Patch] " + ex));
		}
	}
}
