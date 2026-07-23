using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using __Project__.Scripts.FloorPaintSystem;

namespace MultiBoxCarry;

internal static class BoxUtility
{
	public static IQueuableBox GetHeldQueueBox(PlayerObjectHolder holder)
	{
		if ((Object)(object)holder == (Object)null || (Object)(object)holder.CurrentObject == (Object)null)
		{
			return null;
		}

		GameObject current = ((Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
		if ((Object)(object)current == (Object)null)
		{
			return null;
		}

		Box box = current.GetComponent<Box>();
		if ((Object)(object)box != (Object)null)
		{
			return new BoxAdapter(box);
		}

		FurnitureBox furnitureBox = current.GetComponent<FurnitureBox>();
		if ((Object)(object)furnitureBox != (Object)null)
		{
			return new FurnitureBoxAdapter(furnitureBox);
		}

		FloorBox floorBox = current.GetComponent<FloorBox>();
		if ((Object)(object)floorBox != (Object)null)
		{
			return new FloorBoxAdapter(floorBox);
		}

		return null;
	}

	private static FurniturePlacingMode _cachedPlacingMode;
	private static float _nextPlacingModeLookup;

	public static bool IsInPlacingMode(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}

		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		if ((Object)(object)boxInteraction != (Object)null && boxInteraction.m_PlacingMode)
		{
			return true;
		}

		FurnitureBoxInteraction furnitureInteraction = ((Component)player).GetComponent<FurnitureBoxInteraction>();
		if ((Object)(object)furnitureInteraction != (Object)null && furnitureInteraction.m_PlacingMode)
		{
			return true;
		}

		if ((Object)(object)_cachedPlacingMode == (Object)null && Time.unscaledTime >= _nextPlacingModeLookup)
		{
			_cachedPlacingMode = Object.FindObjectOfType<FurniturePlacingMode>();
			_nextPlacingModeLookup = Time.unscaledTime + 1f;
		}

		if ((Object)(object)_cachedPlacingMode != (Object)null && _cachedPlacingMode.IsPlacingMode)
		{
			return true;
		}

		return false;
	}

	public static void HideAndAttachBox(Transform playerTransform, IQueuableBox queueBox, Vector3 localOffset)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)playerTransform == (Object)null))
		{
			queueBox?.HideAndAttach(playerTransform, localOffset);
		}
	}

	internal static void HideAndAttachShared(Transform playerTransform, IQueuableBox queueBox, Vector3 localOffset)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		SetBoxVisible(queueBox, visible: false);
		SetBoxColliders(queueBox, enabled: false);
		SetBoxPhysicsQueued(queueBox);
		queueBox.transform.SetParent(playerTransform, false);
		queueBox.transform.localPosition = localOffset;
		queueBox.transform.localRotation = Quaternion.identity;
	}

	public static void RestoreBox(IQueuableBox queueBox, Transform playerTransform)
	{
		queueBox?.Restore(playerTransform);
	}

	internal static bool IsDestroyed(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return true;
		}

		try
		{
			if (queueBox.Raw is Object unityRaw && (Object)(object)unityRaw == (Object)null)
			{
				return true;
			}

			Transform transform = queueBox.transform;
			return (Object)(object)transform == (Object)null;
		}
		catch
		{
			return true;
		}
	}

	internal static void RestoreShared(Transform playerTransform, IQueuableBox queueBox)
	{
		queueBox.transform.SetParent((Transform)null, true);
		SetBoxVisible(queueBox, visible: true);
		SetBoxColliders(queueBox, enabled: true);
		SetBoxPhysicsWorld(queueBox);
	}

	public static void SetBoxVisible(IQueuableBox queueBox, bool visible)
	{
		if (queueBox == null)
		{
			return;
		}
		foreach (Renderer componentsInChild in ((Component)queueBox.transform).gameObject.GetComponentsInChildren<Renderer>(true))
		{
			if ((Object)(object)componentsInChild != (Object)null)
			{
				componentsInChild.enabled = visible;
			}
		}
	}

	public static void SetBoxColliders(IQueuableBox queueBox, bool enabled)
	{
		if (queueBox == null)
		{
			return;
		}
		foreach (Collider componentsInChild in ((Component)queueBox.transform).GetComponentsInChildren<Collider>(true))
		{
			if ((Object)(object)componentsInChild != (Object)null)
			{
				componentsInChild.enabled = enabled;
			}
		}
	}

	public static void SetBoxPhysicsQueued(IQueuableBox queueBox)
	{
		if (queueBox != null)
		{
			Rigidbody component = ((Component)queueBox.transform).GetComponent<Rigidbody>();
			if (!((Object)(object)component == (Object)null))
			{
				component.linearVelocity = Vector3.zero;
				component.angularVelocity = Vector3.zero;
				component.isKinematic = true;
				component.detectCollisions = false;
				component.interpolation = (RigidbodyInterpolation)0;
			}
		}
	}

	public static void SetBoxPhysicsHeld(IQueuableBox queueBox)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		SetBoxVisible(queueBox, visible: true);
		SetBoxColliders(queueBox, enabled: true);
		Rigidbody component = ((Component)queueBox.transform).GetComponent<Rigidbody>();
		if ((Object)(object)component == (Object)null)
		{
			return;
		}

		component.linearVelocity = Vector3.zero;
		component.angularVelocity = Vector3.zero;
		component.isKinematic = true;
		component.detectCollisions = true;
	}

	public static void SetBoxPhysicsWorld(IQueuableBox queueBox)
	{
		if (queueBox != null)
		{
			Rigidbody component = ((Component)queueBox.transform).GetComponent<Rigidbody>();
			if (!((Object)(object)component == (Object)null))
			{
				component.linearVelocity = Vector3.zero;
				component.angularVelocity = Vector3.zero;
				component.isKinematic = false;
				component.detectCollisions = true;
			}
		}
	}

	internal static void EnableWorldCollisions(IQueuableBox queueBox)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		try
		{
			SetBoxVisible(queueBox, visible: true);
			SetBoxColliders(queueBox, enabled: true);
			Rigidbody body = ((Component)queueBox.transform).GetComponent<Rigidbody>();
			if ((Object)(object)body != (Object)null)
			{
				body.detectCollisions = true;
			}
		}
		catch
		{
		}
	}

	internal static void EnableWorldCollisions(Box box)
	{
		if ((Object)(object)box == (Object)null)
		{
			return;
		}

		EnableWorldCollisions(new BoxAdapter(box));
	}

	internal static void PrepareBoxForWorld(IQueuableBox queueBox)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		try
		{
			Transform transform = queueBox.transform;
			if ((Object)(object)transform != (Object)null && (Object)(object)transform.parent != (Object)null)
			{
				transform.SetParent((Transform)null, true);
			}

			SetBoxVisible(queueBox, visible: true);
			SetBoxColliders(queueBox, enabled: true);
			SetBoxPhysicsWorld(queueBox);
		}
		catch
		{
		}
	}

	internal static void PrepareBoxForWorld(Box box)
	{
		if ((Object)(object)box == (Object)null)
		{
			return;
		}

		PrepareBoxForWorld(new BoxAdapter(box));
	}

	public static Vector3 GetQueueLocalOffset(int index)
	{
		return new Vector3(0f, -3f, -2f);
	}

	internal static void ClearMatchingHighlight(Box box)
	{
		ClearMatchingHighlightCore(box, restoreOccupy: true);
	}

	internal static void ClearMatchingHighlightBeforeQueue(Box box)
	{
		ClearMatchingHighlightCore(box, restoreOccupy: false);
	}

	private static void ClearMatchingHighlightCore(Box box, bool restoreOccupy)
	{
		if ((Object)(object)box == (Object)null)
		{
			return;
		}

		HandHighlightGuard.HighlightBypassDepth++;
		try
		{
			Transform occupyOwner = box.OccupyOwner;
			if ((Object)(object)occupyOwner != (Object)null)
			{
				box.SetOccupy(false, occupyOwner);
			}

			box.m_ActiveHighlightMode = Box.HighlightMode.Display;
			box.UpdateMatchingDisplaySlotsHighlight();

			if (restoreOccupy && (Object)(object)occupyOwner != (Object)null)
			{
				box.SetOccupy(true, occupyOwner);
			}
		}
		catch
		{
		}
		finally
		{
			HandHighlightGuard.HighlightBypassDepth--;
		}
	}

	internal static bool IsQueuedProductBox(Box box)
	{
		if ((Object)(object)box == (Object)null)
		{
			return false;
		}

		PlayerInteraction player = CoopPlayer.GetLocalPlayerInteraction();
		if ((Object)(object)player == (Object)null)
		{
			return false;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return false;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		Box handBox = null;
		if ((Object)(object)holder != (Object)null && (Object)(object)holder.CurrentObject != (Object)null)
		{
			GameObject current = ((Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
			if ((Object)(object)current != (Object)null)
			{
				handBox = current.GetComponent<Box>();
			}
		}

		if ((Object)(object)handBox != (Object)null && (Object)(object)handBox == (Object)(object)box)
		{
			return false;
		}

		for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
		{
			if (inventory.QueuedBoxes[i] is BoxAdapter adapter
				&& (Object)(object)adapter.GetBox() == (Object)(object)box)
			{
				return true;
			}
		}

		return false;
	}
}
