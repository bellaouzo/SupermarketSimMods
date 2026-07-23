using System;
using HarmonyLib;
using UnityEngine;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(BoxInteraction), nameof(BoxInteraction.DropBox), new Type[0])]
internal static class BoxInteraction_DropBox_Guard
{
	private static bool Prefix(BoxInteraction __instance)
	{
		try
		{
			if (!CoopPlayer.InMultiplayer || (Object)(object)__instance == (Object)null)
			{
				return true;
			}

			Box box = __instance.m_Box;
			NetworkBox networkBox = (Object)(object)box != (Object)null ? box.NetworkBox : null;
			Plugin.Log.LogInfo((object)("[MultiBox][dbg] DropBox: box=" + BoxUtility.Describe(box)
				+ " networkBox=" + ((Object)(object)networkBox != (Object)null ? "ok" : "NULL")));
			if ((Object)(object)box == (Object)null || (Object)(object)networkBox != (Object)null)
			{
				return true;
			}

			// Vanilla DropBox_Broadcast dereferences m_Box.NetworkBox and throws an
			// NRE mid-drop when it is missing, leaving the box half-dropped and the
			// hand state corrupted. Do a clean manual drop instead.
			Plugin.Log.LogWarning((object)("[MultiBox] Manual drop (box has no NetworkBox): " + BoxUtility.Describe(box)));
			BoxAdapter adapter = new BoxAdapter(box);
			NetworkBoxUtil.MarkReleased(adapter);

			PlayerObjectHolder holder = ((Component)__instance).GetComponent<PlayerObjectHolder>();
			if ((Object)(object)holder != (Object)null)
			{
				holder.SetNullCurrentObject();
			}

			__instance.m_Box = null;
			__instance.m_PlacingMode = false;

			PlayerInteraction player = ((Component)__instance).GetComponent<PlayerInteraction>();
			if ((Object)(object)player != (Object)null)
			{
				BoxInventoryController.PruneDestroyedQueued(player);
				BoxInventoryController.EnsureHandOrPromotePublic(player);
			}

			return false;
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[MultiBox] DropBox guard failed: " + ex));
			return true;
		}
	}
}
