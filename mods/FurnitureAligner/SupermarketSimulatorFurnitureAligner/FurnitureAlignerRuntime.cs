using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SupermarketSimulatorFurnitureAligner;

internal static class FurnitureAlignerRuntime
{
	private static readonly List<LineRenderer> AlignLines = new List<LineRenderer>();

	private static readonly Dictionary<Transform, Bounds> BoundsCache = new Dictionary<Transform, Bounds>();

	private static readonly Collider[] OverlapBuffer = new Collider[96];

	private static GameObject _alignRoot;

	private static Material _alignMaterial;

	private static Transform _currentFurniture;

	private static Vector3 _nudgeOffset = Vector3.zero;

	private static int _updateFrame = -1;

	private static bool _toggleKeysHandled;

	private static bool _nudgeKeysHandled;

	private static bool _anyPlacingThisFrame;

	internal static bool IsActive => FurnitureAlignerPlugin.Enabled != null && FurnitureAlignerPlugin.Enabled.Value;

	internal static void OnPlacerUpdate(bool placing, Transform currentFurniture)
	{
		BeginFrameIfNeeded();
		if (!_toggleKeysHandled)
		{
			_toggleKeysHandled = true;
			HandleToggleHotkeys();
		}
		if (placing)
		{
			_anyPlacingThisFrame = true;
			if (!_nudgeKeysHandled)
			{
				_nudgeKeysHandled = true;
				HandleNudgeKeys(currentFurniture);
			}
			FurnitureAlignerHints.Sync(true);
		}
	}

	private static void BeginFrameIfNeeded()
	{
		int frame = Time.frameCount;
		if (frame == _updateFrame)
		{
			return;
		}
		if (_updateFrame >= 0 && !_anyPlacingThisFrame)
		{
			ResetPlacementState();
			HideAlignmentLines();
			FurnitureAlignerHints.Sync(false);
		}
		_updateFrame = frame;
		_toggleKeysHandled = false;
		_nudgeKeysHandled = false;
		_anyPlacingThisFrame = false;
	}

	private static void HandleToggleHotkeys()
	{
		CoopHandshake.Tick();
		ToggleEntry(FurnitureAlignerPlugin.ToggleKey.Value, FurnitureAlignerPlugin.Enabled, "Furniture Aligner");
		ToggleExclusiveEntry(FurnitureAlignerPlugin.EdgeAlignKey.Value, FurnitureAlignerPlugin.EdgeAlignEnabled, "Edge alignment", FurnitureAlignerPlugin.CenterLineEnabled, FurnitureAlignerPlugin.GridSnapEnabled);
		ToggleExclusiveEntry(FurnitureAlignerPlugin.CenterLineKey.Value, FurnitureAlignerPlugin.CenterLineEnabled, "Center-line alignment", FurnitureAlignerPlugin.EdgeAlignEnabled, FurnitureAlignerPlugin.GridSnapEnabled);
		ToggleExclusiveEntry(FurnitureAlignerPlugin.GridSnapKey.Value, FurnitureAlignerPlugin.GridSnapEnabled, "Virtual grid snapping", FurnitureAlignerPlugin.EdgeAlignEnabled, FurnitureAlignerPlugin.CenterLineEnabled);
		if (CoopPlacement.InMultiplayer)
		{
			if (FurnitureAlignerPlugin.OutsideKey.Value != KeyCode.None
				&& Input.GetKeyDown(FurnitureAlignerPlugin.OutsideKey.Value))
			{
				FurnitureAlignerPlugin.LogSource.LogInfo("Outside placement toggle ignored in multiplayer.");
			}
		}
		else
		{
			ToggleEntry(FurnitureAlignerPlugin.OutsideKey.Value, FurnitureAlignerPlugin.AllowOutside, "Outside placement");
		}
	}

	internal static Vector3 ApplyAlignment(Transform currentFurniture, Vector3 targetPos)
	{
		try
		{
			return ApplyAlignmentCore(currentFurniture, targetPos);
		}
		catch (Exception ex)
		{
			FurnitureAlignerPlugin.LogSource?.LogWarning("Alignment skipped this frame: " + ex.Message);
			HideAlignmentLines();
			return targetPos;
		}
	}

	private static Vector3 ApplyAlignmentCore(Transform currentFurniture, Vector3 targetPos)
	{
		EnsureCurrentFurniture(currentFurniture);
		bool hasNudge = _nudgeOffset.sqrMagnitude > 1E-06f;
		if (FurnitureAlignerPlugin.GridSnapEnabled.Value && !hasNudge)
		{
			targetPos = SnapToVirtualGrid(targetPos);
		}
		targetPos += _nudgeOffset;
		if (!FurnitureAlignerPlugin.EdgeAlignEnabled.Value && !FurnitureAlignerPlugin.CenterLineEnabled.Value)
		{
			HideAlignmentLines();
			return targetPos;
		}
		if (hasNudge && !FurnitureAlignerPlugin.CenterLineEnabled.Value)
		{
			HideAlignmentLines();
			return targetPos;
		}
		if (!TryGetCachedBounds(currentFurniture, out Bounds bounds))
		{
			return targetPos;
		}
		Vector3 delta = targetPos - currentFurniture.position;
		bounds.center += delta;
		float centerSnap = Mathf.Max(0.01f, ScaledTuningValue(FurnitureAlignerPlugin.SnapDistance));
		float edgeSnap = Mathf.Max(0.01f, ScaledTuningValue(FurnitureAlignerPlugin.EdgeSnapDistance));
		float search = Mathf.Max(Mathf.Max(centerSnap, edgeSnap), ScaledTuningValue(FurnitureAlignerPlugin.SnapSearchRadius));
		float gap = Mathf.Max(0f, ScaledTuningValue(FurnitureAlignerPlugin.SnapGap));
		float radius = Mathf.Max(search, Mathf.Max(centerSnap, edgeSnap) + 1.5f);
		int hitCount = Physics.OverlapSphereNonAlloc(targetPos, radius, OverlapBuffer);
		float bestEdgeX = edgeSnap;
		float bestEdgeZ = edgeSnap;
		bool hasEdgeX = false;
		bool hasEdgeZ = false;
		float snappedX = targetPos.x;
		float snappedZ = targetPos.z;
		float alignX = 0f;
		float alignZ = 0f;
		bool hasCenterX = false;
		bool hasCenterZ = false;
		float centerAlignX = 0f;
		float centerAlignZ = 0f;
		float bestCenterX = centerSnap;
		float bestCenterZ = centerSnap;
		Bounds xTargetBounds = bounds;
		Bounds zTargetBounds = bounds;
		for (int hi = 0; hi < hitCount; hi++)
		{
			Collider hit = OverlapBuffer[hi];
			if ((UnityEngine.Object)(object)hit == (UnityEngine.Object)null || hit.isTrigger)
			{
				continue;
			}
			Transform hitTransform = hit.transform;
			if ((UnityEngine.Object)(object)hitTransform == (UnityEngine.Object)null
				|| hitTransform == currentFurniture
				|| hitTransform.IsChildOf(currentFurniture)
				|| !TryGetPlaceableBounds(hit, currentFurniture, out Bounds other))
			{
				continue;
			}
			if (FurnitureAlignerPlugin.EdgeAlignEnabled.Value && !hasNudge)
			{
				TrySnapEdgeAxis(bounds.min.x, bounds.max.x, other.min.x, other.max.x, targetPos.x, gap, ref snappedX, ref alignX, ref bestEdgeX, ref hasEdgeX);
				TrySnapEdgeAxis(bounds.min.z, bounds.max.z, other.min.z, other.max.z, targetPos.z, gap, ref snappedZ, ref alignZ, ref bestEdgeZ, ref hasEdgeZ);
			}
			if (FurnitureAlignerPlugin.CenterLineEnabled.Value)
			{
				if (TrySnapCenterAxis(bounds.center.x, other.center.x, targetPos.x, ref snappedX, ref centerAlignX, ref bestCenterX, ref hasCenterX))
				{
					xTargetBounds = other;
					hasEdgeX = true;
					alignX = centerAlignX;
				}
				if (TrySnapCenterAxis(bounds.center.z, other.center.z, targetPos.z, ref snappedZ, ref centerAlignZ, ref bestCenterZ, ref hasCenterZ))
				{
					zTargetBounds = other;
					hasEdgeZ = true;
					alignZ = centerAlignZ;
				}
			}
		}
		if (hasEdgeX)
		{
			targetPos.x = snappedX;
		}
		if (hasEdgeZ)
		{
			targetPos.z = snappedZ;
		}
		if (FurnitureAlignerPlugin.CenterLineEnabled.Value && (hasCenterX || hasCenterZ))
		{
			ShowAlignmentLines(hasCenterX, hasCenterZ, centerAlignX, centerAlignZ, bounds, xTargetBounds, zTargetBounds);
		}
		else
		{
			HideAlignmentLines();
		}
		return targetPos;
	}


	internal static void HideAlignmentLines()
	{
		if (_alignRoot != null && _alignRoot.activeSelf)
		{
			_alignRoot.SetActive(false);
		}
	}

	internal static void ResetPlacementState()
	{
		_currentFurniture = null;
		_nudgeOffset = Vector3.zero;
		BoundsCache.Clear();
	}

	private static void ToggleEntry(KeyCode key, ConfigEntry<bool> entry, string label)
	{
		if (entry != null && key != KeyCode.None && Input.GetKeyDown(key))
		{
			entry.Value = !entry.Value;
			FurnitureAlignerPlugin.LogSource.LogInfo(label + ": " + (entry.Value ? "ON" : "OFF"));
			if (entry == FurnitureAlignerPlugin.Enabled && !entry.Value)
			{
				HideAlignmentLines();
			}
			FurnitureAlignerHints.NotifyStateChanged();
		}
	}

	private static void ToggleExclusiveEntry(KeyCode key, ConfigEntry<bool> entry, string label, params ConfigEntry<bool>[] others)
	{
		if (entry == null || key == KeyCode.None || !Input.GetKeyDown(key))
		{
			return;
		}
		entry.Value = !entry.Value;
		if (entry.Value && others != null)
		{
			foreach (ConfigEntry<bool> other in others)
			{
				if (other != null)
				{
					other.Value = false;
				}
			}
		}
		FurnitureAlignerPlugin.LogSource.LogInfo(label + ": " + (entry.Value ? "ON" : "OFF"));
		if (!FurnitureAlignerPlugin.CenterLineEnabled.Value)
		{
			HideAlignmentLines();
		}
		FurnitureAlignerHints.NotifyStateChanged();
	}

	private static void HandleNudgeKeys(Transform currentFurniture)
	{
		EnsureCurrentFurniture(currentFurniture);
		if (currentFurniture == null)
		{
			return;
		}
		if (WasPressed(FurnitureAlignerPlugin.NudgeResetKey.Value))
		{
			_nudgeOffset = Vector3.zero;
			FurnitureAlignerPlugin.LogSource.LogInfo("Manual nudge reset.");
			return;
		}
		Vector3 move = Vector3.zero;
		if (WasPressed(FurnitureAlignerPlugin.NudgeUpKey.Value) || WasPressed(KeyCode.Keypad8))
		{
			move += Vector3.forward;
		}
		if (WasPressed(FurnitureAlignerPlugin.NudgeDownKey.Value) || WasPressed(KeyCode.Keypad2))
		{
			move -= Vector3.forward;
		}
		if (WasPressed(FurnitureAlignerPlugin.NudgeLeftKey.Value) || WasPressed(KeyCode.Keypad4))
		{
			move -= Vector3.right;
		}
		if (WasPressed(FurnitureAlignerPlugin.NudgeRightKey.Value) || WasPressed(KeyCode.Keypad6))
		{
			move += Vector3.right;
		}
		if (move.sqrMagnitude > 0.001f)
		{
			_nudgeOffset += move.normalized * Mathf.Max(0.001f, ScaledTuningValue(FurnitureAlignerPlugin.NudgeStep));
		}
	}

	private static Vector3 SnapToVirtualGrid(Vector3 position)
	{
		float gridSize = Mathf.Max(0.01f, ScaledTuningValue(FurnitureAlignerPlugin.GridSize));
		position.x = SnapValue(position.x, ScaledTuningValue(FurnitureAlignerPlugin.GridOriginX), gridSize);
		position.z = SnapValue(position.z, ScaledTuningValue(FurnitureAlignerPlugin.GridOriginZ), gridSize);
		return position;
	}

	private static float ScaledTuningValue(ConfigEntry<float> entry)
	{
		return entry == null ? 0f : entry.Value * 0.01f;
	}

	private static float SnapValue(float value, float origin, float gridSize)
	{
		return origin + Mathf.Round((value - origin) / gridSize) * gridSize;
	}

	private static bool WasPressed(KeyCode key)
	{
		return key != KeyCode.None && Input.GetKeyDown(key);
	}

	private static void EnsureCurrentFurniture(Transform currentFurniture)
	{
		if (currentFurniture != _currentFurniture)
		{
			_currentFurniture = currentFurniture;
			_nudgeOffset = Vector3.zero;
			BoundsCache.Clear();
		}
	}

	private static bool TrySnapEdgeAxis(float currentMin, float currentMax, float otherMin, float otherMax, float currentCenter, float gap, ref float snappedCenter, ref float alignValue, ref float bestDistance, ref bool hasSnap)
	{
		bool snapped = false;
		snapped |= TrySnapCandidate(otherMin - gap - (currentMax - currentCenter), otherMin, currentCenter, ref snappedCenter, ref alignValue, ref bestDistance, ref hasSnap);
		snapped |= TrySnapCandidate(otherMax + gap - (currentMin - currentCenter), otherMax, currentCenter, ref snappedCenter, ref alignValue, ref bestDistance, ref hasSnap);
		snapped |= TrySnapCandidate(otherMin - (currentMin - currentCenter), otherMin, currentCenter, ref snappedCenter, ref alignValue, ref bestDistance, ref hasSnap);
		snapped |= TrySnapCandidate(otherMax - (currentMax - currentCenter), otherMax, currentCenter, ref snappedCenter, ref alignValue, ref bestDistance, ref hasSnap);
		return snapped;
	}

	private static bool TrySnapCenterAxis(float currentCenterValue, float otherCenter, float targetCenter, ref float snappedCenter, ref float alignValue, ref float bestDistance, ref bool hasSnap)
	{
		return TrySnapCandidate(otherCenter - (currentCenterValue - targetCenter), otherCenter, targetCenter, ref snappedCenter, ref alignValue, ref bestDistance, ref hasSnap);
	}

	private static bool TrySnapCandidate(float candidateCenter, float candidateAlignValue, float currentCenter, ref float snappedCenter, ref float alignValue, ref float bestDistance, ref bool hasSnap)
	{
		float distance = Mathf.Abs(candidateCenter - currentCenter);
		if (distance <= bestDistance)
		{
			bestDistance = distance;
			snappedCenter = candidateCenter;
			alignValue = candidateAlignValue;
			hasSnap = true;
			return true;
		}
		return false;
	}

	private static bool TryGetBounds(Transform root, out Bounds bounds)
	{
		Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
		if (renderers.Length != 0)
		{
			bounds = renderers[0].bounds;
			for (int i = 1; i < renderers.Length; i++)
			{
				if (renderers[i] != null && renderers[i].enabled)
				{
					bounds.Encapsulate(renderers[i].bounds);
				}
			}
			return true;
		}
		Collider[] colliders = root.GetComponentsInChildren<Collider>();
		if (colliders.Length != 0)
		{
			bounds = colliders[0].bounds;
			for (int j = 1; j < colliders.Length; j++)
			{
				bounds.Encapsulate(colliders[j].bounds);
			}
			return true;
		}
		bounds = new Bounds(root.position, Vector3.one);
		return false;
	}

	private static bool TryGetPlaceableBounds(Collider collider, Transform currentFurniture, out Bounds bounds)
	{
		Furniture furniture = collider.GetComponentInParent<Furniture>();
		if (furniture != null && furniture.transform != null && TryGetCachedBounds(furniture.transform, out bounds))
		{
			return true;
		}
		bounds = default;
		Transform best = null;
		Bounds bestBounds = default;
		Transform current = collider.transform;
		while (current != null && current != currentFurniture && !current.IsChildOf(currentFurniture) && !IsSceneSurfaceName(current.name))
		{
			if (TryGetCachedRendererBounds(current, out Bounds candidate) && IsPlaceableSized(candidate))
			{
				best = current;
				bestBounds = candidate;
			}
			current = current.parent;
		}
		if (best != null)
		{
			bounds = bestBounds;
			return true;
		}
		return false;
	}

	private static bool TryGetCachedBounds(Transform root, out Bounds bounds)
	{
		if (BoundsCache.TryGetValue(root, out bounds))
		{
			return true;
		}
		if (TryGetBounds(root, out bounds))
		{
			BoundsCache[root] = bounds;
			return true;
		}
		return false;
	}

	private static bool TryGetCachedRendererBounds(Transform root, out Bounds bounds)
	{
		if (BoundsCache.TryGetValue(root, out bounds))
		{
			return true;
		}
		if (TryGetRendererBounds(root, out bounds))
		{
			BoundsCache[root] = bounds;
			return true;
		}
		return false;
	}

	private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
	{
		Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
		if (renderers.Length == 0)
		{
			bounds = default;
			return false;
		}
		bool started = false;
		bounds = new Bounds(root.position, Vector3.zero);
		foreach (Renderer renderer in renderers)
		{
			if (renderer == null || !renderer.enabled)
			{
				continue;
			}
			if (!started)
			{
				bounds = renderer.bounds;
				started = true;
			}
			else
			{
				bounds.Encapsulate(renderer.bounds);
			}
		}
		return started;
	}

	private static bool IsPlaceableSized(Bounds bounds)
	{
		Vector3 size = bounds.size;
		if (size.x < 0.15f || size.z < 0.15f || size.y < 0.15f)
		{
			return false;
		}
		return size.x <= 8f && size.z <= 8f && size.y <= 6f;
	}

	private static bool IsSceneSurfaceName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		string text = name.ToLowerInvariant();
		return text.Contains("wall")
			|| text.Contains("floor")
			|| text.Contains("ground")
			|| text.Contains("ceiling")
			|| text.Contains("roof")
			|| text.Contains("terrain")
			|| text.Contains("street")
			|| text.Contains("road")
			|| text.Contains("sidewalk");
	}

	private static void ShowAlignmentLines(bool hasX, bool hasZ, float alignX, float alignZ, Bounds currentBounds, Bounds xTargetBounds, Bounds zTargetBounds)
	{
		EnsureAlignLines();
		for (int i = 0; i < AlignLines.Count; i++)
		{
			AlignLines[i].gameObject.SetActive(false);
		}
		int index = 0;
		float y = currentBounds.min.y + 0.04f;
		if (hasX)
		{
			float zMin = Mathf.Min(currentBounds.min.z, xTargetBounds.min.z) - 0.35f;
			float zMax = Mathf.Max(currentBounds.max.z, xTargetBounds.max.z) + 0.35f;
			SetAlignLine(index++, new Vector3(alignX, y, zMin), new Vector3(alignX, y, zMax));
		}
		if (hasZ)
		{
			float xMin = Mathf.Min(currentBounds.min.x, zTargetBounds.min.x) - 0.35f;
			float xMax = Mathf.Max(currentBounds.max.x, zTargetBounds.max.x) + 0.35f;
			SetAlignLine(index++, new Vector3(xMin, y, alignZ), new Vector3(xMax, y, alignZ));
		}
	}

	private static void EnsureAlignLines()
	{
		if (_alignRoot == null)
		{
			_alignRoot = new GameObject("FurnitureAligner_AlignLines");
			UnityEngine.Object.DontDestroyOnLoad(_alignRoot);
			Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Hidden/Internal-Colored");
			_alignMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
			if (_alignMaterial != null)
			{
				_alignMaterial.color = new Color(0.1f, 1f, 0.35f, 0.85f);
			}
			CreateAlignLine();
			CreateAlignLine();
		}
		if (!_alignRoot.activeSelf)
		{
			_alignRoot.SetActive(true);
		}
	}


	private static void CreateAlignLine()
	{
		GameObject lineObject = new GameObject("AlignLine");
		lineObject.transform.SetParent(_alignRoot.transform, false);
		LineRenderer line = lineObject.AddComponent<LineRenderer>();
		line.useWorldSpace = true;
		line.positionCount = 2;
		line.startWidth = 0.045f;
		line.endWidth = 0.045f;
		line.material = _alignMaterial;
		line.startColor = _alignMaterial.color;
		line.endColor = _alignMaterial.color;
		line.gameObject.SetActive(false);
		AlignLines.Add(line);
	}

	private static void SetAlignLine(int index, Vector3 start, Vector3 end)
	{
		if (index >= AlignLines.Count)
		{
			CreateAlignLine();
		}
		LineRenderer line = AlignLines[index];
		line.SetPosition(0, start);
		line.SetPosition(1, end);
		line.startColor = _alignMaterial.color;
		line.endColor = _alignMaterial.color;
		line.gameObject.SetActive(true);
	}
}
