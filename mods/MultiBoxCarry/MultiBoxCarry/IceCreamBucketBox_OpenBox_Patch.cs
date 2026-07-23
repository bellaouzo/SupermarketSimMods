using System;
using HarmonyLib;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(IceCreamBucketBox))]
[HarmonyPatch("OpenBox", new Type[] { })]
internal static class IceCreamBucketBox_OpenBox_Patch
{
	[HarmonyPrefix]
	private static void Prefix()
	{
		try
		{
			OnThrowMessanger.WriteMessage("box");
			Plugin.Log.LogInfo((object)"[MultiBox] IceCreamBucketBox.OpenBox detected.");
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[IceCreamBucketBox_OpenBox_Patch] " + ex));
		}
	}
}
