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
			if ((Object)(object)__instance == (Object)null || (Object)(object)_droppingBox == (Object)null)
			{
				_droppingBox = null;
				return;
			}

			PlayerInteraction player = ((Component)__instance).GetComponent<PlayerInteraction>();
			if (!CoopPlayer.IsLocal(player))
			{
				_droppingBox = null;
				return;
			}

			Box dropped = _droppingBox;
			_droppingBox = null;

			if (BoxUtility.IsOnHoldPoint(dropped, __instance))
			{
				BoxInventoryController.RestoreHeldAfterFailedDrop(player, dropped);
				return;
			}

			NetworkBoxUtil.ClearOccupyFlags(new BoxAdapter(dropped));

			// Do NOT null m_Box/CurrentObject or promote here: DropObject is called
			// from inside vanilla BoxInteraction.DropBox, which still needs m_Box
			// for its network broadcast and cleanup after this returns. Vanilla
			// clears its own state; AutoRefill recovers and promotes next frame.
		}
		catch (Exception ex)
		{
			_droppingBox = null;
			Plugin.Log.LogError((object)("[PlayerObjectHolder_DropObject_Patch] " + ex));
		}
	}
}
