using UnityEngine;

namespace MultiBoxCarry;

internal static class BoxUtility
{
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

	internal static void PrepareForHandPickup(IQueuableBox queueBox, Transform playerTransform)
	{
		if (queueBox == null || (Object)(object)playerTransform == (Object)null)
		{
			return;
		}

		if (queueBox is BoxAdapter adapter)
		{
			Box box = adapter.GetBox();
			if ((Object)(object)box != (Object)null)
			{
				box.SetOccupy(false, playerTransform);
			}
		}

		queueBox.transform.SetParent((Transform)null, true);
		queueBox.transform.position = playerTransform.position + playerTransform.forward * 0.6f + Vector3.up * 0.35f;
		queueBox.transform.rotation = playerTransform.rotation;
		SetBoxVisible(queueBox, visible: true);
		SetBoxColliders(queueBox, enabled: true);
		SetBoxPhysicsHandReady(queueBox);
	}

	internal static void RestoreToWorld(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return;
		}

		if (queueBox is BoxAdapter adapter)
		{
			Box box = adapter.GetBox();
			if ((Object)(object)box != (Object)null)
			{
				box.SetOccupy(false, null);
			}
		}

		queueBox.transform.SetParent((Transform)null, true);
		SetBoxVisible(queueBox, visible: true);
		SetBoxColliders(queueBox, enabled: true);
		SetBoxPhysicsWorld(queueBox);
	}

	internal static void SetBoxPhysicsHandReady(IQueuableBox queueBox)
	{
		if (queueBox == null)
		{
			return;
		}

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
