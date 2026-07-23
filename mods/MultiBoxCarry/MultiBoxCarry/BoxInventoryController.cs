using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MultiBoxCarry;

internal static class BoxInventoryController
{
	internal static bool SuppressAutoRefill;

	private static float _promoteBackoffUntil;

	public static bool TryQueueBox(PlayerInteraction player, IQueuableBox heldBox, IQueuableBox targetBox)
	{
		if ((Object)(object)player == (Object)null || heldBox == null || targetBox == null)
		{
			return false;
		}
		if (!CoopPlayer.IsLocal(player))
		{
			return false;
		}

		if (CoopPlayer.InMultiplayer)
		{
			if (!CoopHandshake.PeersMatch)
			{
				const string mismatch = "MultiBoxCarry version mismatch with host — multi-carry blocked.";
				Plugin.Log.LogWarning((object)mismatch);
				QueuePickUpPatch.ShowWarningMessage(mismatch);
				return false;
			}

			if (!IsProductBox(heldBox) || !IsProductBox(targetBox))
			{
				const string msg = "Furniture/floor boxes cannot be multi-carried in co-op";
				Plugin.Log.LogWarning((object)msg);
				QueuePickUpPatch.ShowWarningMessage(msg);
				return false;
			}
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
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
				bool flag = IsSameProduct(heldBox, targetBox);
				bool flag2 = Keyboard.current != null && ((ButtonControl)Keyboard.current.leftShiftKey).isPressed;
				if (flag && !flag2)
				{
					return false;
				}
				SoftUnhandLocal(player);
				if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
				{
					TryRestoreHand(player, heldBox);
					return false;
				}
				BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
				NetworkBoxUtil.MarkQueued(heldBox);
				rackSlot.InstantInteract();
				EnsureHandOrPromote(player);
				return true;
			}

			SoftUnhandLocal(player);
			if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
			{
				TryRestoreHand(player, heldBox);
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

			EnsureHandOrPromote(player);
			return true;
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	public static bool TryPromoteNextBox(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null || !CoopPlayer.IsLocal(player))
		{
			return true;
		}

		if (Time.unscaledTime < _promoteBackoffUntil)
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

		if (BoxUtility.IsDestroyed(queuableBox))
		{
			NetworkBoxUtil.MarkReleased(queuableBox);
			return true;
		}

		if (!PromoteBox(player, queuableBox))
		{
			_promoteBackoffUntil = Time.unscaledTime + 0.2f;
			return true;
		}

		return false;
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

				SoftUnhandLocal(player);
				inventory.AddRaw(heldBox);
			}
			else
			{
				targetBox = inventory.TakeAt(inventory.Count - 1);
				if (targetBox == null)
				{
					return false;
				}

				SoftUnhandLocal(player);
				inventory.InsertAt(0, heldBox);
			}

			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(0));
			NetworkBoxUtil.MarkQueued(heldBox);
			if (!PromoteBox(player, targetBox))
			{
				EnsureHandOrPromote(player);
			}

			return true;
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

			SoftUnhandLocal(player);
			if (!inventory.InsertAt(queueIndex, heldBox))
			{
				inventory.AddRaw(heldBox);
			}

			BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(queueIndex));
			NetworkBoxUtil.MarkQueued(heldBox);
			if (!PromoteBox(player, targetBox))
			{
				EnsureHandOrPromote(player);
			}

			return true;
		}
		finally
		{
			SuppressAutoRefill = false;
		}
	}

	internal static void PruneDestroyedQueued(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return;
		}

		for (int i = inventory.QueuedBoxes.Count - 1; i >= 0; i--)
		{
			IQueuableBox queued = inventory.QueuedBoxes[i];
			if (queued == null || BoxUtility.IsDestroyed(queued))
			{
				NetworkBoxUtil.MarkReleased(queued);
				inventory.TakeAt(i);
			}
		}
	}

	private static void SoftUnhandLocal(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)boxInteraction != (Object)null)
		{
			boxInteraction.m_Box = null;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		if ((Object)(object)holder != (Object)null)
		{
			holder.SetNullCurrentObject();
		}
	}

	private static void EnsureHandOrPromote(PlayerInteraction player)
	{
		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		if ((Object)(object)holder != (Object)null && (Object)(object)holder.CurrentObject != (Object)null)
		{
			return;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return;
		}

		_promoteBackoffUntil = 0f;
		TryPromoteNextBox(player);
	}

	private static bool PromoteBox(PlayerInteraction player, IQueuableBox queuableBox)
	{
		if ((Object)(object)player == (Object)null || queuableBox == null)
		{
			return false;
		}

		if (BoxUtility.IsDestroyed(queuableBox))
		{
			NetworkBoxUtil.MarkReleased(queuableBox);
			return false;
		}

		NetworkBoxUtil.PrepareForHandPickup(queuableBox);
		BoxUtility.RestoreBox(queuableBox, ((Component)player).transform);
		IInteractable interactable = GetInteractable(queuableBox);
		if (interactable == null || BoxUtility.IsDestroyed(queuableBox))
		{
			if (BoxUtility.IsDestroyed(queuableBox))
			{
				NetworkBoxUtil.MarkReleased(queuableBox);
				return false;
			}

			RequeueHidden(player, queuableBox);
			return false;
		}

		player.SetCurrentInteractable(interactable);
		player.Interact(false, false);

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox handAfter = BoxUtility.GetHeldQueueBox(holder);
		if (handAfter == null || handAfter.Raw != queuableBox.Raw)
		{
			if (BoxUtility.IsDestroyed(queuableBox))
			{
				NetworkBoxUtil.MarkReleased(queuableBox);
				return false;
			}

			if (!TryForceHand(player, queuableBox))
			{
				RequeueHidden(player, queuableBox);
				return false;
			}
		}

		if (queuableBox is BoxAdapter promotedAdapter)
		{
			Box promoted = promotedAdapter.GetBox();
			BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
			if ((Object)(object)boxInteraction != (Object)null && (Object)(object)promoted != (Object)null)
			{
				boxInteraction.m_Box = promoted;
				if (promoted.m_ActiveHighlightMode == Box.HighlightMode.None)
				{
					promoted.m_ActiveHighlightMode = Box.HighlightMode.Display;
				}

				promoted.UpdateMatchingDisplaySlotsHighlight();
			}
		}

		BoxUtility.SetBoxPhysicsHeld(queuableBox);
		NetworkBoxUtil.MarkHeld(queuableBox);
		ReflowQueuedBoxes(player);
		return true;
	}

	private static bool TryForceHand(PlayerInteraction player, IQueuableBox queuableBox)
	{
		if (queuableBox is not BoxAdapter adapter)
		{
			return false;
		}

		Box box = adapter.GetBox();
		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)box == (Object)null || (Object)(object)holder == (Object)null)
		{
			return false;
		}

		try
		{
			Transform holdPoint = (Object)(object)holder.m_ObjectHolder != (Object)null
				? holder.m_ObjectHolder
				: ((Component)player).transform;
			BoxUtility.SetBoxPhysicsHeld(queuableBox);
			queuableBox.transform.SetParent(holdPoint, false);
			queuableBox.transform.localPosition = Vector3.zero;
			queuableBox.transform.localRotation = Quaternion.identity;
			box.SetOccupy(true, ((Component)player).transform);
			holder.CurrentObject = ((Component)box).gameObject;
			if ((Object)(object)boxInteraction != (Object)null)
			{
				boxInteraction.m_Box = box;
			}

			NetworkBoxUtil.MarkHeld(queuableBox);
			return BoxUtility.GetHeldQueueBox(holder) != null;
		}
		catch
		{
			return false;
		}
	}

	private static void RequeueHidden(PlayerInteraction player, IQueuableBox queuableBox)
	{
		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory != null)
		{
			bool alreadyQueued = false;
			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				if (inventory.QueuedBoxes[i] != null && inventory.QueuedBoxes[i].Raw == queuableBox.Raw)
				{
					alreadyQueued = true;
					break;
				}
			}

			if (!alreadyQueued && !inventory.IsFull)
			{
				inventory.InsertAt(0, queuableBox);
			}
		}

		BoxUtility.HideAndAttachBox(((Component)player).transform, queuableBox, BoxUtility.GetQueueLocalOffset(0));
		NetworkBoxUtil.MarkQueued(queuableBox);
		ReflowQueuedBoxes(player);
	}

	private static void TryRestoreHand(PlayerInteraction player, IQueuableBox heldBox)
	{
		if ((Object)(object)player == (Object)null || heldBox == null || BoxUtility.IsDestroyed(heldBox))
		{
			return;
		}

		NetworkBoxUtil.PrepareForHandPickup(heldBox);
		BoxUtility.RestoreBox(heldBox, ((Component)player).transform);
		IInteractable interactable = GetInteractable(heldBox);
		if (interactable == null)
		{
			return;
		}

		player.SetCurrentInteractable(interactable);
		player.Interact(false, false);
		if (BoxUtility.GetHeldQueueBox(((Component)player).GetComponent<PlayerObjectHolder>()) == null)
		{
			TryForceHand(player, heldBox);
		}
	}

	public static void ReflowQueuedBoxes(PlayerInteraction player)
	{
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
			if (queuableBox != null && !BoxUtility.IsDestroyed(queuableBox))
			{
				queuableBox.transform.SetParent(((Component)player).transform, false);
				queuableBox.transform.localPosition = BoxUtility.GetQueueLocalOffset(i);
				queuableBox.transform.localRotation = Quaternion.identity;
			}
		}
	}

	private static bool IsProductBox(IQueuableBox queueBox)
	{
		return queueBox is BoxAdapter;
	}

	private static IInteractable GetInteractable(IQueuableBox queuedBox)
	{
		if (queuedBox == null || queuedBox.Raw == null || BoxUtility.IsDestroyed(queuedBox))
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
		if (queueBox == null || BoxUtility.IsDestroyed(queueBox))
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
