using System;
using __Project__.Scripts.Multiplayer.NetworkInteractions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Photon.Pun;
using UnityEngine;

namespace MultiBoxCarry;

// Vanilla NetworkBoxInteraction.DropBox_Broadcast throws an NRE mid-drop for
// mod-held boxes (unchecked dereference chain), which strands the box and leaves
// placing mode stuck. Replace it with a null-safe version of the same three
// steps: re-enable box transform sync, clear IsNetworkOccupied, RPC DropBox_RPC
// to the other players.
[HarmonyPatch(typeof(NetworkBoxInteraction), nameof(NetworkBoxInteraction.DropBox_Broadcast))]
internal static class NetworkBoxInteraction_DropBroadcast_Guard
{
	private static bool Prefix(NetworkBoxInteraction __instance)
	{
		if (!CoopPlayer.InMultiplayer || (Object)(object)__instance == (Object)null)
		{
			return true;
		}

		try
		{
			BoxInteraction boxInteraction = __instance.BoxInteraction;
			if ((Object)(object)boxInteraction == (Object)null)
			{
				boxInteraction = ((Component)__instance).GetComponent<BoxInteraction>();
			}

			Box box = (Object)(object)boxInteraction != (Object)null ? boxInteraction.m_Box : null;
			NetworkBox networkBox = (Object)(object)box != (Object)null ? box.NetworkBox : null;
			if ((Object)(object)networkBox != (Object)null)
			{
				try
				{
					networkBox.DisableNetworkTransformSync(false);
				}
				catch (Exception ex)
				{
					Plugin.Log.LogWarning((object)("[MultiBox] Drop broadcast: transform sync re-enable failed: " + ex.Message));
				}

				networkBox.IsNetworkOccupied = false;
			}
			else
			{
				Plugin.Log.LogWarning((object)("[MultiBox] Drop broadcast: "
					+ ((Object)(object)box == (Object)null ? "no held box" : "box has no NetworkBox: " + BoxUtility.Describe(box))
					+ " — skipped box network reset."));
			}

			string userId = null;
			try
			{
				userId = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.UserId : null;
			}
			catch
			{
			}

			PhotonView view = ((Component)__instance).GetComponentInParent<PhotonView>();
			if ((Object)(object)view != (Object)null && !string.IsNullOrEmpty(userId))
			{
				Il2CppReferenceArray<Il2CppSystem.Object> args = new Il2CppReferenceArray<Il2CppSystem.Object>(1);
				args[0] = new Il2CppSystem.Object(IL2CPP.ManagedStringToIl2Cpp(userId));
				view.RPC("DropBox_RPC", RpcTarget.Others, args);
			}
			else
			{
				Plugin.Log.LogWarning((object)("[MultiBox] Drop broadcast: "
					+ ((Object)(object)view == (Object)null ? "player PhotonView missing" : "local UserId missing")
					+ " — peers not notified of drop."));
			}

			Plugin.Log.LogInfo((object)("[MultiBox][dbg] Safe drop broadcast done for " + BoxUtility.Describe(box)));
			return false;
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[MultiBox] Safe drop broadcast failed: " + ex));
			return false;
		}
	}
}
