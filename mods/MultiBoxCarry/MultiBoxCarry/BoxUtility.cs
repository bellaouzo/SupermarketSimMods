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
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
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

	public static void SetBoxPhysicsWorld(IQueuableBox queueBox)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
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

	public static Vector3 GetQueueLocalOffset(int index)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3(0f, 0.45f, 0.2f);
	}
}
