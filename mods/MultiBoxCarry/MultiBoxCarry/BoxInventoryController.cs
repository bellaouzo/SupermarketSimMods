using UnityEngine;

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
				SoftUnhandLocal(player);
				if (!inventory.Enqueue(heldBox, targetBox.GetProduct()))
				{
					TryRestoreHand(player, heldBox);
					return false;
				}
				BoxUtility.HideAndAttachBox(((Component)player).transform, heldBox, BoxUtility.GetQueueLocalOffset(count));
				NetworkBoxUtil.MarkQueued(heldBox);
				rackSlot.InstantInteract();
				EnsurePresentedHeld(player);
				FinishQueuePickup(player);
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
			if (CoopPlayer.InMultiplayer)
			{
				// Vanilla Interact misfires in a Photon room (the networked holder
				// still thinks we're holding, so it resolves as a drop instead of a
				// pickup — boxes end up released at the player's feet). Take the
				// target into the hand ourselves; TryForceHand does the occupy
				// broadcast via MarkHeld.
				NetworkBoxUtil.PrepareForHandPickup(targetBox);
				bool forced = TryForceHand(player, targetBox);
				PlayerObjectHolder dbgHolder = ((Component)player).GetComponent<PlayerObjectHolder>();
				Plugin.Log.LogInfo((object)("[MultiBox][dbg] queue-pickup: queued=" + BoxUtility.Describe(heldBox)
					+ " target=" + BoxUtility.Describe(targetBox)
					+ " forceHand=" + (forced ? "ok" : "FAILED")
					+ " hand=" + BoxUtility.Describe(BoxUtility.GetHeldQueueBox(dbgHolder))));
				if (!forced)
				{
					EnsureHandOrPromote(player);
				}

				EnsurePresentedHeld(player);
				SanitizeHandVisuals(player);
				return true;
			}

			IInteractable component = ((Component)targetBox.transform).GetComponent<IInteractable>();
			if (component != null)
			{
				player.SetCurrentInteractable(component);
				player.Interact(false, false);
			}

			EnsurePresentedHeld(player);
			FinishQueuePickup(player);
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

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox held = BoxUtility.GetHeldQueueBox(holder);
		if (held is BoxAdapter adapter)
		{
			Box box = adapter.GetBox();
			Transform occupyOwner = (Object)(object)box != (Object)null ? box.OccupyOwner : null;
			if ((Object)(object)box != (Object)null && (Object)(object)occupyOwner != (Object)null)
			{
				box.SetOccupy(false, occupyOwner);
			}
		}

		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)boxInteraction != (Object)null)
		{
			boxInteraction.m_Box = null;
			boxInteraction.m_PlacingMode = false;
		}

		if ((Object)(object)holder != (Object)null)
		{
			holder.SetNullCurrentObject();
		}
	}

	private static void FinishQueuePickup(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		if (CoopPlayer.InMultiplayer)
		{
			PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
			if ((Object)(object)holder == (Object)null || (Object)(object)holder.CurrentObject == (Object)null)
			{
				// In a Photon room the vanilla pickup we just initiated completes
				// asynchronously. Promoting from the queue now races it: the arriving
				// box gets swept into the queue and the hand ends up holding a hidden
				// box. Hold promote back instead — RecoverDesyncedHold force-hands the
				// box when it arrives, and AutoRefill promotes after the grace window
				// if the pickup never lands.
				_promoteBackoffUntil = Time.unscaledTime + 0.6f;
				return;
			}

			EnsurePresentedHeld(player);
			SanitizeHandVisuals(player);
			return;
		}

		EnsureHandOrPromote(player);
		EnsurePresentedHeld(player);
		SanitizeHandVisuals(player);
	}

	private static void EnsurePresentedHeld(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox held = BoxUtility.GetHeldQueueBox(holder);
		if (held != null)
		{
			BoxUtility.EnsurePresented(held);
			BoxUtility.SetBoxPhysicsHeld(held);
		}
	}

	internal static void EnsureHandOrPromotePublic(PlayerInteraction player)
	{
		EnsureHandOrPromote(player);
	}

	private static readonly System.Collections.Generic.List<Box> _holdPointBoxes = new System.Collections.Generic.List<Box>(8);

	internal static void RestoreHeldAfterFailedDrop(PlayerInteraction player, Box box)
	{
		if ((Object)(object)player == (Object)null || (Object)(object)box == (Object)null)
		{
			return;
		}

		TryForceHand(player, new BoxAdapter(box));
		SanitizeHandVisuals(player);
		Plugin.Log.LogWarning((object)"[MultiBox] Place/drop failed — restored box to hand instead of consuming it.");
	}

	internal static void SanitizeHandVisuals(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		if ((Object)(object)holder == (Object)null)
		{
			return;
		}

		Box handBox = null;
		IQueuableBox held = BoxUtility.GetHeldQueueBox(holder);
		if (held is BoxAdapter heldAdapter)
		{
			handBox = heldAdapter.GetBox();

			// Hand and queue must be disjoint: if the hand box is also queued,
			// ReflowQueuedBoxes re-hides it every frame (invisible held box) and
			// the HUD double-counts it.
			BoxInventory heldInventory = PlayerInventoryManager.GetInventory(player);
			if (heldInventory != null && heldInventory.RemoveByRaw(handBox))
			{
				Plugin.Log.LogWarning((object)("[MultiBox] Removed hand box from queue (hand/queue overlap): "
					+ BoxUtility.Describe(handBox)));
			}

			BoxUtility.EnsurePresented(held);
		}

		BoxUtility.CollectHoldPointBoxes(holder, _holdPointBoxes);
		for (int i = 0; i < _holdPointBoxes.Count; i++)
		{
			Box extra = _holdPointBoxes[i];
			if ((Object)(object)extra == (Object)null
				|| (Object)(object)extra == (Object)(object)handBox)
			{
				continue;
			}

			IQueuableBox adapter = new BoxAdapter(extra);
			if (BoxUtility.IsQueuedProductBox(extra))
			{
				BoxUtility.HideAndAttachBox(((Component)player).transform, adapter, BoxUtility.GetQueueLocalOffset(0));
				NetworkBoxUtil.MarkQueued(adapter);
				continue;
			}

			BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
			if (inventory != null && !inventory.IsFull)
			{
				bool alreadyQueued = false;
				for (int q = 0; q < inventory.QueuedBoxes.Count; q++)
				{
					if (inventory.QueuedBoxes[q] != null && BoxUtility.SameBox(inventory.QueuedBoxes[q].Raw, adapter.Raw))
					{
						alreadyQueued = true;
						break;
					}
				}

				if (!alreadyQueued)
				{
					inventory.AddRaw(adapter);
				}

				BoxUtility.HideAndAttachBox(((Component)player).transform, adapter, BoxUtility.GetQueueLocalOffset(0));
				NetworkBoxUtil.MarkQueued(adapter);
				Plugin.Log.LogWarning((object)("[MultiBox] Moved overlapping hand box back into queue: "
					+ BoxUtility.Describe(extra) + " hand=" + BoxUtility.Describe(handBox)));
			}
			else
			{
				NetworkBoxUtil.MarkReleased(adapter);
				Plugin.Log.LogWarning((object)"[MultiBox] Released overlapping hand box to world (queue full).");
			}
		}

		ReflowQueuedBoxes(player);
	}

	internal static void RecoverDesyncedHold(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null || !CoopPlayer.IsLocal(player))
		{
			return;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)holder == (Object)null)
		{
			return;
		}

		GameObject current = null;
		if ((Object)(object)holder.CurrentObject != (Object)null)
		{
			current = ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
		}

		Box currentBox = (Object)(object)current != (Object)null ? current.GetComponent<Box>() : null;
		if ((Object)(object)currentBox != (Object)null)
		{
			if ((Object)(object)boxInteraction != (Object)null
				&& (Object)(object)boxInteraction.m_Box != (Object)(object)currentBox)
			{
				boxInteraction.m_Box = currentBox;
			}

			SanitizeHandVisuals(player);
			return;
		}

		Box orphan = BoxUtility.FindOrphanHandBox(holder);
		if ((Object)(object)orphan != (Object)null)
		{
			TryForceHand(player, new BoxAdapter(orphan));
			if ((Object)(object)boxInteraction != (Object)null)
			{
				boxInteraction.m_PlacingMode = false;
			}

			SanitizeHandVisuals(player);
			Plugin.Log.LogWarning((object)"[MultiBox] Recovered orphaned held box after hand desync.");
			return;
		}

		if ((Object)(object)boxInteraction != (Object)null)
		{
			if ((Object)(object)boxInteraction.m_Box != (Object)null)
			{
				boxInteraction.m_Box = null;
			}

			if (boxInteraction.m_PlacingMode)
			{
				boxInteraction.m_PlacingMode = false;
				Plugin.Log.LogWarning((object)"[MultiBox] Cleared stuck placing mode with empty hands.");
			}
		}

		SanitizeHandVisuals(player);
	}

	private static void EnsureHandOrPromote(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		if (BoxUtility.IsInPlacingMode(player))
		{
			return;
		}

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

		if (!CoopPlayer.InMultiplayer)
		{
			// In a Photon room vanilla Interact can resolve as a drop (networked
			// holder state); rely on TryForceHand below instead.
			player.SetCurrentInteractable(interactable);
			player.Interact(false, false);
		}

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
		SanitizeHandVisuals(player);
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
			queuableBox.transform.localScale = Vector3.one;
			BoxUtility.EnsurePresented(queuableBox);
			if ((Object)(object)boxInteraction != (Object)null)
			{
				boxInteraction.m_Box = box;
				boxInteraction.m_PlacingMode = false;
			}

			NetworkBoxUtil.MarkHeld(queuableBox);
			return BoxUtility.GetHeldQueueBox(holder) != null;
		}
		catch (System.Exception ex)
		{
			Plugin.Log.LogError(
				(object)("[MultiBox] TryForceHand FAILED for " + BoxUtility.Describe(queuableBox) + ": " + ex));
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
				if (inventory.QueuedBoxes[i] != null && BoxUtility.SameBox(inventory.QueuedBoxes[i].Raw, queuableBox.Raw))
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

		if (!CoopPlayer.InMultiplayer)
		{
			player.SetCurrentInteractable(interactable);
			player.Interact(false, false);
		}

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
				BoxUtility.SetBoxColliders(queuableBox, enabled: false);
				BoxUtility.SetBoxPhysicsQueued(queuableBox);
				queuableBox.transform.SetParent(((Component)player).transform, false);
				queuableBox.transform.localPosition = BoxUtility.GetQueueLocalOffset(i);
				queuableBox.transform.localRotation = Quaternion.identity;
				queuableBox.transform.localScale = Vector3.zero;
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

}
