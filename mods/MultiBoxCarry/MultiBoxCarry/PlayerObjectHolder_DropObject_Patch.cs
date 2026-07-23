using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerObjectHolder), "DropObject")]
internal static class PlayerObjectHolder_DropObject_Patch
{
	private static Box _droppingBox;

	[HarmonyPrefix]
	private static void Prefix(PlayerObjectHolder __instance)
	{
		_droppingBox = null;
		try
		{
			if ((Object)(object)__instance == (Object)null || (Object)(object)__instance.CurrentObject == (Object)null)
			{
				return;
			}

			PlayerInteraction player = ((Component)__instance).GetComponent<PlayerInteraction>();
			if (!CoopPlayer.IsLocal(player))
			{
				return;
			}

			GameObject current = ((Il2CppObjectBase)__instance.CurrentObject).TryCast<GameObject>();
			if ((Object)(object)current == (Object)null)
			{
				return;
			}

			_droppingBox = current.GetComponent<Box>();
			if ((Object)(object)_droppingBox != (Object)null)
			{
				BoxUtility.EnableWorldCollisions(_droppingBox);
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[PlayerObjectHolder_DropObject_Patch.Prefix] " + ex));
		}
	}

	[HarmonyPostfix]
	private static void Postfix(PlayerObjectHolder __instance)
	{
		try
		{
			if ((Object)(object)__instance == (Object)null)
			{
				return;
			}

			PlayerInteraction player = ((Component)__instance).GetComponent<PlayerInteraction>();
			if (!CoopPlayer.IsLocal(player))
			{
				_droppingBox = null;
				return;
			}

			if ((Object)(object)_droppingBox != (Object)null)
			{
				BoxUtility.PrepareBoxForWorld(_droppingBox);
				NetworkBoxUtil.MarkReleased(new BoxAdapter(_droppingBox));
				_droppingBox = null;
			}

			__instance.SetNullCurrentObject();
			BoxInventoryController.PruneDestroyedQueued(player);
		}
		catch (Exception ex)
		{
			_droppingBox = null;
			Plugin.Log.LogError((object)("[PlayerObjectHolder_DropObject_Patch] " + ex));
		}
	}
}
