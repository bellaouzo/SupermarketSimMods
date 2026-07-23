using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MultiBoxCarry;

internal static class BoxInventoryController
{
	internal static bool SuppressAutoRefill;

	public static bool TryQueueBox(PlayerInteraction player, IQueuableBox heldBox, IQueuableBox targetBox)
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)player == (Object)null || heldBox == null || targetBox == null)
		{
			return false;
		}
		if (!CoopPlayer.IsLocal(player))
		{
			return false;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsFull)
		{
			Plugin.Log.LogInfo((object)"[InventoryController] Queue full.");
			return false;
		}
		int count = inventory.Count;
		RackSlot rackSlot = GetRackSlot(targetBox);
		if ((Object)(object)rackSlot != (Object)null)
		{
			bool flag = IsSameProduct(heldBox, targetBox);
			bool flag2 = Keyboard.current != null && ((ButtonControl)Keyboard.current.leftShiftKey).isPressed;
			if (flag && !flag2)
			{
				return false;
			}
			heldBox.Drop(player);
			if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
			{
				return false;
			}
			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
			NetworkBoxUtil.MarkQueued(heldBox);
			rackSlot.InstantInteract();
			return true;
		}
		heldBox.Drop(player);
		if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
		{
			return false;
		}
		BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
		NetworkBoxUtil.MarkQueued(heldBox);
		IInteractable component = ((Component)targetBox.transform).GetComponent<IInteractable>();
		if (component != null)
		{
			player.SetCurrentInteractable(component);
			player.Interact(false, false);
		}
		return true;
	}

	public static bool TryPromoteNextBox(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null || !CoopPlayer.IsLocal(player))
		{
			return true;
		}
		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return true;
		}
		IQueuableBox queuableBox = inventory.Dequeue();
		if (queuableBox == null)
		{
			return true;
		}
		return !PromoteBox(player, queuableBox);
	}

	public static bool TryCycleBoxes(PlayerInteraction player, int direction)
	{
		if ((Object)(object)player == (Object)null || direction == 0 || !CoopPlayer.IsLocal(player))
		{
			return false;
		}

		if (BoxUtility.IsInPlacingMode(player))
		{
			return false;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return false;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox heldBox = BoxUtility.GetHeldQueueBox(holder);
		if (heldBox == null)
		{
			return false;
		}

		SuppressAutoRefill = true;
		try
		{
			IQueuableBox targetBox;
			if (direction > 0)
			{
				targetBox = inventory.TakeAt(0);
				if (targetBox == null)
				{
					return false;
				}

				heldBox.Drop(player);
				inventory.AddRaw(heldBox);
			}
			else
			{
				targetBox = inventory.TakeAt(inventory.Count - 1);
				if (targetBox == null)
				{
					return false;
				}

				heldBox.Drop(player);
				inventory.InsertAt(0, heldBox);
			}

			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(0));
			return PromoteBox(player, targetBox);
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	public static bool TrySwitchToQueueIndex(PlayerInteraction player, int queueIndex)
	{
		if ((Object)(object)player == (Object)null || !CoopPlayer.IsLocal(player))
		{
			return false;
		}

		if (BoxUtility.IsInPlacingMode(player))
		{
			return false;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || queueIndex < 0 || queueIndex >= inventory.Count)
		{
			return false;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox heldBox = BoxUtility.GetHeldQueueBox(holder);
		if (heldBox == null)
		{
			return false;
		}

		SuppressAutoRefill = true;
		try
		{
			IQueuableBox targetBox = inventory.TakeAt(queueIndex);
			if (targetBox == null)
			{
				return false;
			}

			heldBox.Drop(player);
			if (!inventory.InsertAt(queueIndex, heldBox))
			{
				inventory.AddRaw(heldBox);
			}

			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(queueIndex));
			return PromoteBox(player, targetBox);
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	private static bool PromoteBox(PlayerInteraction player, IQueuableBox queuableBox)
	{
		if ((Object)(object)player == (Object)null || queuableBox == null)
		{
			return false;
		}

		NetworkBoxUtil.MarkReleased(queuableBox);
		BoxUtility.RestoreBox(queuableBox, ((Component)player).transform);
		IInteractable interactable = GetInteractable(queuableBox);
		if (interactable == null)
		{
			ReflowQueuedBoxes(player);
			return false;
		}

		player.SetCurrentInteractable(interactable);
		player.Interact(false, false);
		ReflowQueuedBoxes(player);
		return true;
	}

	public static void ReflowQueuedBoxes(PlayerInteraction player)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)player == (Object)null)
		{
			return;
		}
		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
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
			}
		}
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
		if (queueBox == null)
		{
			return null;
		}
		if (!(queueBox is BoxAdapter))
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
		if (heldBox == null || targetBox == null)
		{
			return false;
		}
		if (!(heldBox is BoxAdapter boxAdapter))
		{
			return false;
		}
		if (!(targetBox is BoxAdapter boxAdapter2))
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
