using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MultiBoxCarry;

internal static class BoxInventoryController
{
	internal static bool SuppressAutoRefill;

	public static bool TryQueueBox(PlayerInteraction player, IQueuableBox heldBox, IQueuableBox targetBox)
	{
		if ((Object)(object)player == (Object)null || heldBox == null || targetBox == null)
		{
			return false;
		}

		BoxInventory inventory = PlayerInventoryManager.Inventory;
		if (inventory == null || inventory.IsFull)
		{
			Plugin.Log.LogInfo((object)"[InventoryController] Queue full.");
			return false;
		}

		int count = inventory.Count;
		RackSlot rackSlot = GetRackSlot(targetBox);
		SuppressAutoRefill = true;
		try
		{
			if ((Object)(object)rackSlot != (Object)null)
			{
				bool sameProduct = IsSameProduct(heldBox, targetBox);
				bool shiftHeld = Keyboard.current != null && ((ButtonControl)Keyboard.current.leftShiftKey).isPressed;
				if (sameProduct && !shiftHeld)
				{
					return false;
				}

				heldBox.Drop(player);
				if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
				{
					return false;
				}

				BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
				NetworkBoxSync.MarkQueued(heldBox);
				rackSlot.InstantInteract();
				NetworkBoxSync.MarkHeld(targetBox);
				return true;
			}

			heldBox.Drop(player);
			if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
			{
				return false;
			}

			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
			NetworkBoxSync.MarkQueued(heldBox);
			IInteractable component = ((Component)targetBox.transform).GetComponent<IInteractable>();
			if (component != null)
			{
				player.SetCurrentInteractable(component);
				player.Interact(false, false);
			}

			NetworkBoxSync.MarkHeld(targetBox);
			return true;
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	public static bool TryPromoteNextBox(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return true;
		}

		BoxInventory inventory = PlayerInventoryManager.Inventory;
		if (inventory == null || inventory.IsEmpty)
		{
			return true;
		}

		IQueuableBox queuableBox = inventory.Dequeue();
		if (queuableBox == null)
		{
			return true;
		}

		SuppressAutoRefill = true;
		try
		{
			NetworkBoxSync.MarkReleased(queuableBox);
			BoxUtility.PrepareForHandPickup(queuableBox, ((Component)player).transform);

			IInteractable interactable = GetInteractable(queuableBox);
			if (interactable == null)
			{
				BoxUtility.RestoreToWorld(queuableBox);
				NetworkBoxSync.MarkReleased(queuableBox);
				ReflowQueuedBoxes(player);
				return true;
			}

			player.SetCurrentInteractable(interactable);
			player.Interact(false, false);

			if (!IsHoldingBox(player, queuableBox))
			{
				BoxUtility.RestoreToWorld(queuableBox);
				NetworkBoxSync.MarkReleased(queuableBox);
				ReflowQueuedBoxes(player);
				return true;
			}

			NetworkBoxSync.MarkHeld(queuableBox);
			ReflowQueuedBoxes(player);
			return false;
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	public static void ReflowQueuedBoxes(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		BoxInventory inventory = PlayerInventoryManager.Inventory;
		if (inventory == null || inventory.IsEmpty)
		{
			return;
		}

		for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
		{
			IQueuableBox queuableBox = inventory.QueuedBoxes[i];
			if (queuableBox != null)
			{
				queuableBox.transform.SetParent(((Component)player).transform, false);
				queuableBox.transform.localPosition = BoxUtility.GetQueueLocalOffset(i);
				queuableBox.transform.localRotation = Quaternion.identity;
				BoxUtility.SetBoxVisible(queuableBox, visible: false);
				BoxUtility.SetBoxColliders(queuableBox, enabled: false);
				BoxUtility.SetBoxPhysicsQueued(queuableBox);
			}
		}
	}

	private static bool IsHoldingBox(PlayerInteraction player, IQueuableBox box)
	{
		if ((Object)(object)player == (Object)null || box?.Raw == null)
		{
			return false;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		if ((Object)(object)holder == (Object)null || (Object)(object)holder.CurrentObject == (Object)null)
		{
			return false;
		}

		GameObject current = ((Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
		if ((Object)(object)current == (Object)null)
		{
			return false;
		}

		Component component = box.Raw as Component;
		if ((Object)(object)component == (Object)null)
		{
			return false;
		}

		return (Object)(object)current == (Object)(object)((Component)component).gameObject;
	}

	private static IInteractable GetInteractable(IQueuableBox queuedBox)
	{
		if (queuedBox == null || queuedBox.Raw == null)
		{
			return null;
		}

		object raw = queuedBox.Raw;
		Component val = (Component)((raw is Component) ? raw : null);
		if (val != null)
		{
			return val.GetComponent<IInteractable>();
		}

		return null;
	}

	private static RackSlot GetRackSlot(IQueuableBox queueBox)
	{
		if (queueBox == null || !(queueBox is BoxAdapter))
		{
			return null;
		}

		Transform parent = queueBox.transform.parent;
		if ((Object)(object)parent == (Object)null)
		{
			return null;
		}

		return ((Component)parent).GetComponent<RackSlot>();
	}

	private static bool IsSameProduct(IQueuableBox heldBox, IQueuableBox targetBox)
	{
		if (heldBox is not BoxAdapter boxAdapter || targetBox is not BoxAdapter boxAdapter2)
		{
			return false;
		}

		Box box = boxAdapter.GetBox();
		Box box2 = boxAdapter2.GetBox();
		if ((Object)(object)box.Product == (Object)null || (Object)(object)box2.Product == (Object)null)
		{
			return false;
		}

		return (Object)(object)box.Product == (Object)(object)box2.Product;
	}
}
