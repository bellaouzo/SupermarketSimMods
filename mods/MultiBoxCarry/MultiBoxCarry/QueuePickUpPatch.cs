using System;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.InputSystem;
using __Project__.Scripts.FloorPaintSystem;

namespace MultiBoxCarry;

[HarmonyPatch(typeof(PlayerInteraction), "OnUse")]
internal static class QueuePickUpPatch
{
	private const float SWAP_RAY_DISTANCE = 2.5f;

	[HarmonyPrefix]
	private static bool Prefix(PlayerInteraction __instance, InputAction.CallbackContext context)
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
			if (!CoopPlayer.IsLocal(__instance))
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
			IQueuableBox heldQueueBox = BoxUtility.GetHeldQueueBox(component);
			if (heldQueueBox == null)
			{
				return true;
			}
			if (BoxUtility.IsInPlacingMode(__instance))
			{
				return true;
			}
			IQueuableBox queuableBox = FindTargetQueueBox(mainCamera, __instance, heldQueueBox);
			if (queuableBox == null)
			{
				return true;
			}
			BoxInventory inventory = PlayerInventoryManager.GetInventory(__instance);
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
		IQueuableBox fromInteractable = GetQueueBoxFromInteractable(player.CurrentInteractable);
		if (IsValidPickupTarget(fromInteractable, heldBox, player))
		{
			return fromInteractable;
		}

		RaycastHit playerHit = player.m_Hit;
		Collider hitCollider = playerHit.collider;
		if ((Object)(object)hitCollider != (Object)null)
		{
			IQueuableBox fromHit = GetQueueBoxFromCollider(hitCollider);
			if (IsValidPickupTarget(fromHit, heldBox, player))
			{
				return fromHit;
			}
		}

		Ray ray = new Ray(((Component)cam).transform.position, ((Component)cam).transform.forward);
		Il2CppStructArray<RaycastHit> hits = Physics.RaycastAll(ray, SWAP_RAY_DISTANCE, -1, QueryTriggerInteraction.Collide);
		if (hits == null || hits.Length == 0)
		{
			return null;
		}

		int hitCount = hits.Length;
		int[] order = new int[hitCount];
		for (int i = 0; i < hitCount; i++)
		{
			order[i] = i;
		}

		for (int i = 0; i < hitCount - 1; i++)
		{
			int best = i;
			for (int j = i + 1; j < hitCount; j++)
			{
				if (hits[order[j]].distance < hits[order[best]].distance)
				{
					best = j;
				}
			}

			if (best != i)
			{
				int temp = order[i];
				order[i] = order[best];
				order[best] = temp;
			}
		}

		for (int i = 0; i < hitCount; i++)
		{
			RaycastHit hit = hits[order[i]];
			Collider collider = hit.collider;
			if ((Object)(object)collider == (Object)null)
			{
				continue;
			}

			Transform transform = ((Component)collider).transform;
			if ((Object)(object)transform == (Object)null
				|| transform.IsChildOf(((Component)player).transform)
				|| ((Component)collider).CompareTag("Player"))
			{
				continue;
			}

			Transform parent = transform.parent;
			if ((Object)(object)parent != (Object)null && ((Object)parent).name.Contains("Rack Manager"))
			{
				break;
			}

			IQueuableBox queueBoxFromCollider = GetQueueBoxFromCollider(collider);
			if (IsValidPickupTarget(queueBoxFromCollider, heldBox, player))
			{
				return queueBoxFromCollider;
			}
		}

		return null;
	}

	private static bool IsValidPickupTarget(IQueuableBox target, IQueuableBox heldBox, PlayerInteraction player)
	{
		if (target == null || AreSameUnderlyingObject(target, heldBox) || IsQueueBoxOccupied(target))
		{
			return false;
		}

		Transform transform = target.transform;
		if ((Object)(object)transform == (Object)null)
		{
			return false;
		}

		if (transform.IsChildOf(((Component)player).transform))
		{
			return false;
		}

		return true;
	}

	private static IQueuableBox GetQueueBoxFromInteractable(IInteractable interactable)
	{
		if (interactable == null)
		{
			return null;
		}

		Il2CppObjectBase obj = ((Il2CppObjectBase)(object)interactable);
		Box box = obj.TryCast<Box>();
		if ((Object)(object)box != (Object)null)
		{
			return new BoxAdapter(box);
		}

		FurnitureBox furnitureBox = obj.TryCast<FurnitureBox>();
		if ((Object)(object)furnitureBox != (Object)null)
		{
			return new FurnitureBoxAdapter(furnitureBox);
		}

		FloorBox floorBox = obj.TryCast<FloorBox>();
		if ((Object)(object)floorBox != (Object)null)
		{
			return new FloorBoxAdapter(floorBox);
		}

		Component component = obj.TryCast<Component>();
		if ((Object)(object)component != (Object)null)
		{
			return GetQueueBoxFromComponent(component);
		}

		return null;
	}

	private static IQueuableBox GetQueueBoxFromCollider(Collider collider)
	{
		if ((Object)(object)collider == (Object)null)
		{
			return null;
		}

		return GetQueueBoxFromComponent((Component)collider);
	}

	private static IQueuableBox GetQueueBoxFromComponent(Component component)
	{
		if ((Object)(object)component == (Object)null)
		{
			return null;
		}

		Box box = component.GetComponentInParent<Box>();
		if ((Object)(object)box != (Object)null)
		{
			return new BoxAdapter(box);
		}

		FurnitureBox furnitureBox = component.GetComponentInParent<FurnitureBox>();
		if ((Object)(object)furnitureBox != (Object)null)
		{
			return new FurnitureBoxAdapter(furnitureBox);
		}

		FloorBox floorBox = component.GetComponentInParent<FloorBox>();
		if ((Object)(object)floorBox != (Object)null)
		{
			return new FloorBoxAdapter(floorBox);
		}

		BoxInteraction boxInteraction = component.GetComponentInParent<BoxInteraction>();
		if ((Object)(object)boxInteraction != (Object)null && (Object)(object)boxInteraction.m_Box != (Object)null)
		{
			return new BoxAdapter(boxInteraction.m_Box);
		}

		FurnitureBoxInteraction furnitureInteraction = component.GetComponentInParent<FurnitureBoxInteraction>();
		if ((Object)(object)furnitureInteraction != (Object)null)
		{
			FurnitureBox furnitureFromInteraction = furnitureInteraction.CurrentFurnitureBox;
			if ((Object)(object)furnitureFromInteraction != (Object)null)
			{
				return new FurnitureBoxAdapter(furnitureFromInteraction);
			}
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
		return queueBox?.IsOccupied() ?? false;
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
