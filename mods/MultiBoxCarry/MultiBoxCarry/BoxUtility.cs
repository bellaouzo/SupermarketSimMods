using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using __Project__.Scripts.FloorPaintSystem;

namespace MultiBoxCarry;

internal static class BoxUtility
{
	public static IQueuableBox GetHeldQueueBox(PlayerObjectHolder holder)
	{
		if ((Object)(object)holder == (Object)null)
		{
			return null;
		}

		if ((Object)(object)holder.CurrentObject != (Object)null)
		{
			GameObject current = ((Il2CppObjectBase)holder.CurrentObject).TryCast<GameObject>();
			IQueuableBox fromCurrent = GetQueueBoxFromGameObject(current);
			if (fromCurrent != null)
			{
				return fromCurrent;
			}
		}

		BoxInteraction boxInteraction = ((Component)holder).GetComponent<BoxInteraction>();
		if ((Object)(object)boxInteraction != (Object)null && (Object)(object)boxInteraction.m_Box != (Object)null)
		{
			return new BoxAdapter(boxInteraction.m_Box);
		}

		return null;
	}

	private static IQueuableBox GetQueueBoxFromGameObject(GameObject current)
	{
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

	internal static string Describe(Box box)
	{
		if ((Object)(object)box == (Object)null)
		{
			return "<null>";
		}

		try
		{
			return ((Object)box).name + "#" + ((Object)box).GetInstanceID();
		}
		catch
		{
			return "<gone>";
		}
	}

	internal static string Describe(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return "<null>";
		}

		return queueBox is BoxAdapter adapter ? Describe(adapter.GetBox()) : queueBox.GetType().Name;
	}

	internal static bool SameBox(object a, object b)
	{
		if (ReferenceEquals(a, b))
		{
			return true;
		}

		// Il2Cpp interop can hand out distinct managed wrappers for the same
		// native object; compare as Unity objects (native identity) when possible.
		if (a is Object unityA && b is Object unityB)
		{
			return (Object)(object)unityA == (Object)(object)unityB;
		}

		return false;
	}

	internal static bool IsLocalInventoryBox(Box box, PlayerInteraction player)
	{
		if ((Object)(object)box == (Object)null || (Object)(object)player == (Object)null)
		{
			return false;
		}

		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		IQueuableBox held = GetHeldQueueBox(holder);
		if (held is BoxAdapter heldAdapter && (Object)(object)heldAdapter.GetBox() == (Object)(object)box)
		{
			return true;
		}

		return IsQueuedProductBox(box);
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
		if (queueBox == null || IsDestroyed(queueBox) || (Object)(object)playerTransform == (Object)null)
		{
			return;
		}

		SetBoxColliders(queueBox, enabled: false);
		SetBoxPhysicsQueued(queueBox);
		Transform transform = queueBox.transform;
		transform.SetParent(playerTransform, false);
		transform.localPosition = localOffset;
		transform.localRotation = Quaternion.identity;
		transform.localScale = Vector3.zero;
		SetBoxVisible(queueBox, visible: true);
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
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		Transform transform = queueBox.transform;
		transform.SetParent((Transform)null, true);
		transform.localScale = Vector3.one;
		EnsurePresented(queueBox);
		SetBoxPhysicsWorld(queueBox);
	}

	public static void SetBoxVisible(IQueuableBox queueBox, bool visible)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		GameObject root = ((Component)queueBox.transform).gameObject;
		if (!root.activeSelf)
		{
			root.SetActive(true);
		}

		Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			if ((Object)(object)renderer != (Object)null)
			{
				renderer.enabled = visible;
			}
		}
	}

	internal static void EnsurePresented(IQueuableBox queueBox)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return;
		}

		Transform transform = queueBox.transform;
		if ((Object)(object)transform != (Object)null)
		{
			Vector3 scale = transform.localScale;
			if (scale.x < 0.01f || scale.y < 0.01f || scale.z < 0.01f)
			{
				transform.localScale = Vector3.one;
			}
		}

		SetBoxVisible(queueBox, visible: true);
		SetBoxColliders(queueBox, enabled: true);
	}

	internal static void EnsurePresented(Box box)
	{
		if ((Object)(object)box == (Object)null)
		{
			return;
		}

		EnsurePresented(new BoxAdapter(box));
	}

	internal static bool HasHiddenRenderer(IQueuableBox queueBox)
	{
		if (queueBox == null || IsDestroyed(queueBox))
		{
			return false;
		}

		Renderer[] renderers = ((Component)queueBox.transform).gameObject.GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];
			if ((Object)(object)renderer != (Object)null && !renderer.enabled)
			{
				return true;
			}
		}

		return false;
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
				Transform parent = transform.parent;
				bool keepParent = (Object)(object)parent != (Object)null
					&& ((Object)(object)((Component)parent).GetComponent<RackSlot>() != (Object)null
						|| (Object)(object)((Component)parent).GetComponentInParent<Rack>() != (Object)null);
				if (!keepParent)
				{
					transform.SetParent((Transform)null, true);
				}
			}

			EnsurePresented(queueBox);
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
		int i = Mathf.Max(0, index);
		return new Vector3(0f, -3f - i * 0.35f, -2f - i * 0.75f);
	}

	internal static bool IsOnHoldPoint(Box box, PlayerObjectHolder holder)
	{
		if ((Object)(object)box == (Object)null || (Object)(object)holder == (Object)null)
		{
			return false;
		}

		Transform boxTransform = ((Component)box).transform;
		Transform holdPoint = holder.m_ObjectHolder;
		if ((Object)(object)boxTransform == (Object)null || (Object)(object)holdPoint == (Object)null)
		{
			return false;
		}

		Transform parent = boxTransform.parent;
		return (Object)(object)parent != (Object)null
			&& ((Object)(object)parent == (Object)(object)holdPoint || boxTransform.IsChildOf(holdPoint));
	}

	internal static Box FindOrphanHandBox(PlayerObjectHolder holder)
	{
		if ((Object)(object)holder == (Object)null)
		{
			return null;
		}

		Transform holdPoint = holder.m_ObjectHolder;
		if ((Object)(object)holdPoint == (Object)null)
		{
			return null;
		}

		Box[] boxes = ((Component)holdPoint).GetComponentsInChildren<Box>(true);
		if (boxes == null || boxes.Length == 0)
		{
			return null;
		}

		for (int i = 0; i < boxes.Length; i++)
		{
			Box box = boxes[i];
			if ((Object)(object)box == (Object)null || IsQueuedProductBox(box))
			{
				continue;
			}

			if (IsOnHoldPoint(box, holder))
			{
				return box;
			}
		}

		return null;
	}

	internal static void CollectHoldPointBoxes(PlayerObjectHolder holder, List<Box> results)
	{
		results.Clear();
		if ((Object)(object)holder == (Object)null || (Object)(object)holder.m_ObjectHolder == (Object)null)
		{
			return;
		}

		Box[] boxes = ((Component)holder.m_ObjectHolder).GetComponentsInChildren<Box>(true);
		if (boxes == null)
		{
			return;
		}

		for (int i = 0; i < boxes.Length; i++)
		{
			Box box = boxes[i];
			if ((Object)(object)box != (Object)null)
			{
				results.Add(box);
			}
		}
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
