using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerObjectHolder), "DropObject")]
internal static class PlayerObjectHolder_DropObject_Patch
{
	private static Box _pendingRelease;

	[HarmonyPrefix]
	private static void Prefix(PlayerObjectHolder __instance)
	{
		_pendingRelease = null;
		if (BoxInventoryController.SuppressAutoRefill)
		{
			return;
		}

		try
		{
			if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.CurrentObject == (Object)null)
			{
				return;
			}

			GameObject current = __instance.CurrentObject.TryCast<GameObject>();
			if ((Object)(object)current == (Object)null)
			{
				return;
			}

			_pendingRelease = current.GetComponent<Box>();
		}
		catch
		{
		}
	}

	[HarmonyPostfix]
	private static void Postfix(PlayerObjectHolder __instance)
	{
		try
		{
			if ((Object)(object)_pendingRelease != (Object)null)
			{
				NetworkBoxSync.MarkReleased(new BoxAdapter(_pendingRelease));
				_pendingRelease = null;
			}

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
