using System;
using HarmonyLib;
using SupermarketSimulator.Clerk;
using UnityEngine;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch]
public static class ClerkRecoveryPatch
{
	private static float _nextGlobalCleanup;

	[HarmonyPatch(typeof(Clerk), nameof(Clerk.SetRestockerManagementData))]
	[HarmonyPostfix]
	public static void Clerk_SetRestockerManagementData_Postfix(Clerk __instance, RestockerManagementData restockerData)
	{
		if ((UnityEngine.Object)(object)__instance == (UnityEngine.Object)null || restockerData == null)
		{
			return;
		}
		if (!restockerData.IsActive)
		{
			ReleaseBoxesOwnedBy(__instance.transform);
			return;
		}
		RunRecoveryPass();
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireRestocker")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireRestocker_Postfix(int restockerID)
	{
		RunRecoveryPass();
	}

	internal static void OnSceneReady()
	{
		RunRecoveryPass();
	}

	internal static void Tick()
	{
		if (Time.unscaledTime < _nextGlobalCleanup)
		{
			return;
		}
		_nextGlobalCleanup = Time.unscaledTime + 15f;
		RunRecoveryPass();
	}

	private static void RunRecoveryPass()
	{
		Box[] boxes = null;
		try
		{
			boxes = UnityEngine.Object.FindObjectsOfType<Box>();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Restocker recovery box scan failed: {ex.Message}");
			return;
		}

		if (boxes == null || boxes.Length == 0)
		{
			return;
		}

		ReleaseOrphanOccupiedBoxes(boxes);
		DropFloatingStorageBoxes(boxes);
	}

	private static void ReleaseBoxesOwnedBy(Transform owner)
	{
		if ((UnityEngine.Object)(object)owner == (UnityEngine.Object)null)
		{
			return;
		}
		Box[] boxes = UnityEngine.Object.FindObjectsOfType<Box>();
		if (boxes == null)
		{
			return;
		}
		int released = 0;
		foreach (Box box in boxes)
		{
			if ((UnityEngine.Object)(object)box == (UnityEngine.Object)null || !box.IsBoxOccupied)
			{
				continue;
			}
			if ((UnityEngine.Object)(object)box.OccupyOwner == (UnityEngine.Object)(object)owner)
			{
				box.SetOccupy(false, null);
				released++;
			}
		}
		if (released > 0)
		{
			Plugin.LogInfo($"Restocker recovery: released {released} box occupy lock(s) for inactive clerk.");
		}
	}

	private static void ReleaseOrphanOccupiedBoxes(Box[] boxes)
	{
		try
		{
			int released = 0;
			foreach (Box box in boxes)
			{
				if ((UnityEngine.Object)(object)box == (UnityEngine.Object)null || !box.IsBoxOccupied)
				{
					continue;
				}
				Transform owner = box.OccupyOwner;
				if ((UnityEngine.Object)(object)owner == (UnityEngine.Object)null || !owner.gameObject.activeInHierarchy)
				{
					box.SetOccupy(false, null);
					released++;
				}
			}
			if (released > 0)
			{
				Plugin.LogInfo($"Restocker recovery: cleared {released} orphaned box occupy lock(s).");
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Restocker occupy cleanup failed: {ex.Message}");
		}
	}

	private static void DropFloatingStorageBoxes(Box[] boxes)
	{
		try
		{
			int dropped = 0;
			foreach (Box box in boxes)
			{
				if ((UnityEngine.Object)(object)box == (UnityEngine.Object)null)
				{
					continue;
				}
				if (box.Racked || box.OnPlacementArea || box.IsBoxOccupied)
				{
					continue;
				}
				Transform t = box.transform;
				if ((UnityEngine.Object)(object)t == (UnityEngine.Object)null || !t.gameObject.activeInHierarchy)
				{
					continue;
				}
				if (t.parent != null && t.parent.GetComponentInParent<Clerk>() != null)
				{
					continue;
				}
				if (t.position.y < 2.8f)
				{
					continue;
				}
				Rigidbody body = box.RigidBody ?? box.m_RigidBody;
				if ((UnityEngine.Object)(object)body == (UnityEngine.Object)null)
				{
					continue;
				}
				bool stuckMidair = body.isKinematic || !body.useGravity;
				if (!stuckMidair && body.velocity.sqrMagnitude > 0.01f)
				{
					continue;
				}
				box.DropBox();
				dropped++;
			}
			if (dropped > 0)
			{
				Plugin.LogInfo($"Restocker recovery: dropped {dropped} floating storage box(es).");
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Restocker floating-box cleanup failed: {ex.Message}");
		}
	}
}
