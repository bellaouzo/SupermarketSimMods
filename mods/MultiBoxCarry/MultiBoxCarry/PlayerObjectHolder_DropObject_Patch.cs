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

			GameObject current = null;
			if ((Object)(object)__instance.CurrentObject != (Object)null)
			{
				current = ((Il2CppObjectBase)__instance.CurrentObject).TryCast<GameObject>();
			}

			bool currentIsDropped = (Object)(object)current != (Object)null
				&& (Object)(object)current.GetComponent<Box>() == (Object)(object)dropped;
			if ((Object)(object)current == (Object)null || currentIsDropped)
			{
				__instance.SetNullCurrentObject();
				BoxInteraction boxInteraction = ((Component)__instance).GetComponent<BoxInteraction>();
				if ((Object)(object)boxInteraction != (Object)null)
				{
					if ((Object)(object)boxInteraction.m_Box == (Object)(object)dropped)
					{
						boxInteraction.m_Box = null;
					}

					if (boxInteraction.m_PlacingMode)
					{
						boxInteraction.m_PlacingMode = false;
					}
				}
			}

			BoxInventoryController.PruneDestroyedQueued(player);
			BoxInventoryController.SanitizeHandVisuals(player);
			if (!BoxUtility.IsInPlacingMode(player))
			{
				BoxInventoryController.EnsureHandOrPromotePublic(player);
			}
		}
		catch (Exception ex)
		{
			_droppingBox = null;
			Plugin.Log.LogError((object)("[PlayerObjectHolder_DropObject_Patch] " + ex));
		}
	}
}
