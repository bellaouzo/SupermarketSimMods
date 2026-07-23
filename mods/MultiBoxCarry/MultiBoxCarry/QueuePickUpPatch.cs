using System;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using __Project__.Scripts.FloorPaintSystem;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "OnUse")]
internal static class QueuePickUpPatch
{
	private const float SWAP_RAY_DISTANCE = 2.5f;

	[HarmonyPrefix]
	private static bool Prefix(PlayerInteraction __instance, CallbackContext context)
	{
		try
		{
			bool flag = false;
			try
			{
				flag = context.started;
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning((object)("[MultiBox] Could not read CallbackContext flags: " + ex));
			}
			if (!flag)
			{
				return true;
			}
			if (!CoopPlayer.IsLocalInteraction(__instance) || !CoopNetwork.PeersMatch)
			{
				return true;
			}
			PlayerObjectHolder component = ((Component)__instance).GetComponent<PlayerObjectHolder>();
			if ((Object)(object)component == (Object)null)
			{
				Plugin.Log.LogInfo((object)"[MultiBox] PlayerObjectHolder not found. Letting vanilla continue.");
				return true;
			}
			Camera mainCamera = __instance.m_MainCamera;
			if ((Object)(object)mainCamera == (Object)null)
			{
				Plugin.Log.LogWarning((object)"[MultiBox] Camera was null. Letting vanilla continue.");
				return true;
			}
			IQueuableBox heldQueueBox = GetHeldQueueBox(component);
			if (heldQueueBox == null)
			{
				return true;
			}
			if (IsInPlacingMode(__instance))
			{
				return true;
			}
			IQueuableBox queuableBox = FindTargetQueueBox(mainCamera, __instance, heldQueueBox);
			if (queuableBox == null)
			{
				return true;
			}
			BoxInventory inventory = PlayerInventoryManager.Inventory;
			if (inventory == null)
			{
				return true;
			}
			if (inventory.IsFull)
			{
				ShowWarningMessage("Max Boxes Reached");
				return true;
			}
			return !BoxInventoryController.TryQueueBox(__instance, heldQueueBox, queuableBox);
		}
		catch (Exception ex2)
		{
			Plugin.Log.LogError((object)("[MultiBox] QueuePickUpPatch Prefix error: " + ex2));
			return true;
		}
	}

	private static IQueuableBox FindTargetQueueBox(Camera cam, PlayerInteraction player, IQueuableBox heldBox)
	{
		Ray ray = new Ray(((Component)cam).transform.position, ((Component)cam).transform.forward);
		RaycastHit[] hits = Physics.RaycastAll(ray, SWAP_RAY_DISTANCE, -1, QueryTriggerInteraction.Collide);
		if (hits == null || hits.Length == 0)
		{
			return null;
		}

		foreach (RaycastHit hit in hits.OrderBy((RaycastHit h) => h.distance))
		{
			if ((Object)(object)hit.collider == (Object)null)
			{
				continue;
			}

			Transform transform = ((Component)hit.collider).transform;
			if ((Object)(object)transform == (Object)null
				|| transform.IsChildOf(((Component)player).transform)
				|| ((Component)hit.collider).CompareTag("Player"))
			{
				continue;
			}

			Transform parent = transform.parent;
			if ((Object)(object)parent != (Object)null && ((Object)parent).name.Contains("Rack Manager"))
			{
				break;
			}

			IQueuableBox queueBoxFromCollider = GetQueueBoxFromCollider(hit.collider);
			if (queueBoxFromCollider != null
				&& !AreSameUnderlyingObject(queueBoxFromCollider, heldBox)
				&& !IsQueueBoxOccupied(queueBoxFromCollider))
			{
				return queueBoxFromCollider;
			}
		}

		return null;
	}

	private static IQueuableBox GetHeldQueueBox(PlayerObjectHolder holder)
	{
		if ((Object)(object)holder == (Object)null || (Object)(object)holder.CurrentObject == (Object)null)
		{
			return null;
		}
		GameObject val = ((Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
		if ((Object)(object)val == (Object)null)
		{
			return null;
		}
		Box component = val.GetComponent<Box>();
		if ((Object)(object)component != (Object)null)
		{
			return new BoxAdapter(component);
		}
		FurnitureBox component2 = val.GetComponent<FurnitureBox>();
		if ((Object)(object)component2 != (Object)null)
		{
			return new FurnitureBoxAdapter(component2);
		}
		FloorBox component3 = val.GetComponent<FloorBox>();
		if ((Object)(object)component3 != (Object)null)
		{
			return new FloorBoxAdapter(component3);
		}
		return null;
	}

	private static IQueuableBox GetQueueBoxFromCollider(Collider collider)
	{
		if ((Object)(object)collider == (Object)null)
		{
			return null;
		}
		Box componentInParent = ((Component)collider).GetComponentInParent<Box>();
		if ((Object)(object)componentInParent != (Object)null)
		{
			return new BoxAdapter(componentInParent);
		}
		FurnitureBox componentInParent2 = ((Component)collider).GetComponentInParent<FurnitureBox>();
		if ((Object)(object)componentInParent2 != (Object)null)
		{
			return new FurnitureBoxAdapter(componentInParent2);
		}
		FloorBox componentInParent3 = ((Component)collider).GetComponentInParent<FloorBox>();
		if ((Object)(object)componentInParent3 != (Object)null)
		{
			return new FloorBoxAdapter(componentInParent3);
		}
		return null;
	}

	private static bool AreSameUnderlyingObject(IQueuableBox a, IQueuableBox b)
	{
		if (a == null || b == null)
		{
			return false;
		}
		return a.Raw == b.Raw;
	}

	private static bool IsQueueBoxOccupied(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return false;
		}

		if (queueBox.IsOccupied())
		{
			return true;
		}

		return NetworkBoxSync.IsNetworkOccupied(queueBox);
	}

	private static bool IsInPlacingMode(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}
		BoxInteraction component = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)component != (Object)null && component.m_PlacingMode)
		{
			return true;
		}
		FurnitureBoxInteraction component2 = ((Component)player).GetComponent<FurnitureBoxInteraction>();
		if ((Object)(object)component2 != (Object)null && component2.m_PlacingMode)
		{
			return true;
		}
		FurniturePlacingMode val = Object.FindObjectOfType<FurniturePlacingMode>();
		if ((Object)(object)val != (Object)null && val.IsPlacingMode)
		{
			return true;
		}
		return false;
	}

	public static void ShowWarningMessage(string message, float duration = 2f)
	{
		try
		{
			WarningCanvas val = Object.FindObjectOfType<WarningCanvas>();
			if ((Object)(object)val != (Object)null)
			{
				val.ShowInteractionWarningWithArgument((InteractionWarningType)35, new string[1] { "" });
			}
		}
		catch (Exception ex)
		{
			Plugin.Log.LogError((object)("[WarningHelper] " + ex));
		}
	}
}
