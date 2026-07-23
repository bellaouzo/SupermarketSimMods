using System;
using HarmonyLib;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

[HarmonyPatch]
public static class BakerCollectPatch
{
	[HarmonyPatch(typeof(CollectBakedProductState), "CollectFromSlot")]
	[HarmonyPrefix]
	public static bool CollectBakedProductState_CollectFromSlot_Prefix(DisplaySlot slot)
	{
		if ((UnityEngine.Object)(object)slot == (UnityEngine.Object)null)
		{
			return false;
		}
		try
		{
			Display display = slot.Display;
			if ((UnityEngine.Object)(object)display == (UnityEngine.Object)null)
			{
				return true;
			}
			if (display.DisplayType == DisplayType.BAKERY_SHELF)
			{
				Plugin.LogInfo("Baker collect blocked: refusing bakery shelf slot while collecting from oven.");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Baker collect oven-guard failed: {ex.Message}");
			return true;
		}
	}

	internal static void EnsureFillOvenEnabled(Baker baker)
	{
		if ((UnityEngine.Object)(object)baker == (UnityEngine.Object)null)
		{
			return;
		}
		try
		{
			BakerManagementData data = baker.ManagementData ?? baker.GetBakerManagementData();
			if (data == null)
			{
				return;
			}
			if (data.IsFillOven)
			{
				return;
			}
			data.IsFillOven = true;
			if (!data.IsCollectFromOven)
			{
				data.IsCollectFromOven = true;
			}
			BakeryManager bakery = BakeryManager.Instance;
			if ((UnityEngine.Object)(object)bakery != (UnityEngine.Object)null)
			{
				bakery.SetBakerManagmentData(data);
			}
			else
			{
				baker.SetBakerManagementData(data);
			}
			Plugin.LogWarn(
				$"Baker[{baker.BakerID}] Fill Oven was OFF — auto-enabled Fill Oven + Collect so he can gather/cook frozen pastries.");
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Baker Fill Oven ensure failed: {ex.Message}");
		}
	}
}
