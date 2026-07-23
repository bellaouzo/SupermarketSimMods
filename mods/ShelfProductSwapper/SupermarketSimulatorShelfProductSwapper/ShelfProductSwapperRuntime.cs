using Object = UnityEngine.Object;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace SupermarketSimulatorShelfProductSwapper;

internal static class ShelfProductSwapperRuntime
{
	internal struct RemoteBoxEntry
	{
		internal int ProductId;

		internal int ProductCount;
	}

	private struct SlotSnapshot
	{
		internal int ProductId;

		internal int Count;

		internal bool HasProduct;

		internal bool HasLabel;

		internal int LabelProductId;

		internal float LabelPrice;

		internal DisplayType ProductDisplayType;

		internal static SlotSnapshot From(DisplaySlot slot)
		{
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			SlotSnapshot result = default(SlotSnapshot);
			result.ProductId = slot.ProductID;
			result.Count = Mathf.Max(0, slot.ProductCount);
			result.HasProduct = slot.HasProduct && result.Count > 0;
			result.HasLabel = slot.Data != null && slot.Data.HasLabel;
			result.LabelProductId = (result.HasLabel ? slot.Data.FirstItemID : (-1));
			result.LabelPrice = slot.Price;
			result.ProductDisplayType = (DisplayType)3;
			if (result.HasProduct)
			{
				ProductSO val = slot.PeekProductSO();
				if ((Object)(object)val != (Object)null)
				{
					result.ProductDisplayType = val.ProductDisplayType;
				}
				else if ((Object)(object)slot.Display != (Object)null)
				{
					result.ProductDisplayType = slot.Display.DisplayType;
				}
			}
			return result;
		}
	}

	private struct SlotProducts
	{
		internal int ProductId = -1;

		internal int Count = 0;

		internal List<Product> Products = new List<Product>();

		public SlotProducts()
		{
		}
	}

	private struct RackSlotSnapshot
	{
		internal int ProductId;

		internal int BoxId;

		internal int BoxCount;

		internal bool HasBox;

		internal bool HasLabel;

		internal bool HasLabelData;

		internal int LabelProductId;

		internal int LabelBoxId;

		internal static RackSlotSnapshot From(RackSlot slot)
		{
			RackSlotSnapshot result = default(RackSlotSnapshot);
			result.ProductId = ((slot.Data != null) ? slot.Data.ProductID : (-1));
			result.BoxId = ((slot.Data != null) ? slot.Data.BoxID : slot.CurrentBoxID);
			result.BoxCount = ((slot.Boxes != null) ? slot.Boxes.Count : 0);
			result.HasBox = slot.HasBox && result.BoxCount > 0;
			result.HasLabelData = TryGetRackLabelData(slot, out var productId, out var boxId);
			result.HasLabel = result.HasLabelData || HasAnyRackLabel(slot);
			result.LabelProductId = (result.HasLabelData ? productId : (-1));
			result.LabelBoxId = (result.HasLabelData ? boxId : (-1));
			return result;
		}
	}

	private struct RackSlotBoxes
	{
		internal int ProductId = -1;

		internal int BoxId = -1;

		internal int Count = 0;

		internal List<Box> Boxes = new List<Box>();

		public RackSlotBoxes()
		{
		}
	}

	private const string MarkerRootName = "ShelfProductSwapper_Markers";

	private static DisplaySlot _selectedSlot;

	private static SlotSnapshot _selectedSnapshot;

	private static DisplaySlot _hoverSlot;

	private static RackSlot _selectedRackSlot;

	private static RackSlotSnapshot _selectedRackSnapshot;

	private static RackSlot _hoverRackSlot;

	private static GameObject _markerRoot;

	private static LineRenderer _hoverMarker;

	private static LineRenderer _selectedMarker;

	private static Material _markerMaterial;

	private static Highlightable _lastHoverHighlightable;

	private static Highlightable _lastSelectedHighlightable;

	private static int _lastHotkeyFrame = -1;

	private static readonly RaycastHit[] RayBuffer = new RaycastHit[64];

	private static readonly Dictionary<int, DisplaySlot> DisplaySlotByCollider = new Dictionary<int, DisplaySlot>();

	private static readonly Dictionary<int, RackSlot> RackSlotByCollider = new Dictionary<int, RackSlot>();

	private static DisplaySlot[] _displaySlotCache;

	private static RackSlot[] _rackSlotCache;

	private static float _slotCacheTime = -1f;

	private static float _nextHoverResolveTime = -1f;

	private static Camera _cachedCamera;

	private const float HoverResolveInterval = 0.1f;

	private const float MaxRayLateralDistance = 0.55f;

	private static DisplaySlot _markerRectSlot;

	private static Vector3 _markerRectCenter;

	private static Vector3 _markerRectRight;

	private static Vector3 _markerRectForward;

	private static float _markerRectHalfW;

	private static float _markerRectHalfD;

	private static DisplaySlot _lastHoverSlot;

	private static RackSlot _lastHoverRackSlot;

	private static DisplaySlot _lastSelectedSlot;

	private static RackSlot _lastSelectedRackSlot;

	private static bool _lastHoverCompatible = true;

	internal static bool HasSelection => (Object)(object)_selectedSlot != (Object)null || (Object)(object)_selectedRackSlot != (Object)null;

	internal static bool HasHoverTarget => (Object)(object)_hoverSlot != (Object)null || (Object)(object)_hoverRackSlot != (Object)null;

	private static bool _disabledIdle;

	internal static void HandleHotkey()
	{
		if (Time.frameCount == _lastHotkeyFrame)
		{
			return;
		}
		_lastHotkeyFrame = Time.frameCount;
		HandleToggleModKey();
		bool enabled = ShelfProductSwapperPlugin.Enabled != null && ShelfProductSwapperPlugin.Enabled.Value;
		if (!enabled)
		{
			if (!_disabledIdle)
			{
				ClearSelection();
				HideMarkers();
				DisplaySlotByCollider.Clear();
				RackSlotByCollider.Clear();
				ShelfProductSwapperHints.Sync(false);
				_disabledIdle = true;
			}
			return;
		}

		_disabledIdle = false;

		KeyCode swapKey = ShelfProductSwapperPlugin.SwapKey.Value;
		bool swapPressed = (int)swapKey != 0 && Input.GetKeyDown(swapKey);
		if (swapPressed || Time.unscaledTime >= _nextHoverResolveTime)
		{
			_nextHoverResolveTime = Time.unscaledTime + HoverResolveInterval;
			ResolveHoverTargets();
		}

		UpdateMarkers();
		HandleToggleIncompatibleKey();
		if (swapPressed)
		{
			if ((Object)(object)_hoverSlot != (Object)null)
			{
				HandleDisplaySlotSwapKey(_hoverSlot);
			}
			else if ((Object)(object)_hoverRackSlot != (Object)null)
			{
				HandleRackSlotSwapKey(_hoverRackSlot);
			}
			else
			{
				ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap: look at a shelf slot, box rack slot, label, or price tag first.");
			}
		}
		ShelfProductSwapperHints.Sync(true);
	}

	private static void ResolveHoverTargets()
	{
		_hoverSlot = null;
		_hoverRackSlot = null;
		Camera main = _cachedCamera;
		if ((Object)(object)main == (Object)null)
		{
			main = Camera.main;
			_cachedCamera = main;
		}
		if ((Object)(object)main == (Object)null)
		{
			return;
		}
		float maxDistance = Mathf.Max(0.5f, ShelfProductSwapperPlugin.RayDistance.Value);
		Ray ray = main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		int hitCount = Physics.RaycastNonAlloc(ray, RayBuffer, maxDistance, -1, QueryTriggerInteraction.Collide);
		float bestDisplayDistance = float.MaxValue;
		float bestRackDistance = float.MaxValue;
		DisplaySlot bestDisplay = null;
		RackSlot bestRack = null;
		for (int i = 0; i < hitCount; i++)
		{
			RaycastHit hit = RayBuffer[i];
			Collider collider = hit.collider;
			if ((Object)(object)collider == (Object)null)
			{
				continue;
			}
			if (hit.distance < bestDisplayDistance && TryGetDisplaySlotFromCollider(collider, out DisplaySlot displaySlot))
			{
				bestDisplayDistance = hit.distance;
				bestDisplay = displaySlot;
			}
			if (hit.distance < bestRackDistance && TryGetRackSlotFromCollider(collider, out RackSlot rackSlot))
			{
				bestRackDistance = hit.distance;
				bestRack = rackSlot;
			}
		}
		if ((Object)(object)bestDisplay != (Object)null)
		{
			_hoverSlot = bestDisplay;
			return;
		}
		if ((Object)(object)bestRack != (Object)null)
		{
			_hoverRackSlot = bestRack;
			return;
		}

		RefreshSlotCaches();
		if (TryGetDisplaySlotNearRay(ray, maxDistance, out _hoverSlot))
		{
			return;
		}

		TryGetRackSlotNearRay(ray, maxDistance, out _hoverRackSlot);
	}

	private static void RefreshSlotCaches()
	{
		if (_displaySlotCache != null && _rackSlotCache != null && Time.unscaledTime < _slotCacheTime)
		{
			return;
		}
		_displaySlotCache = Object.FindObjectsOfType<DisplaySlot>();
		_rackSlotCache = Object.FindObjectsOfType<RackSlot>();
		_slotCacheTime = Time.unscaledTime + 8f;
	}

	private static bool TryGetDisplaySlotNearRay(Ray ray, float maxDistance, out DisplaySlot slot)
	{
		slot = null;
		DisplaySlot[] slots = _displaySlotCache;
		if (slots == null || slots.Length == 0)
		{
			return false;
		}
		float bestScore = float.MaxValue;
		for (int i = 0; i < slots.Length; i++)
		{
			DisplaySlot candidate = slots[i];
			if ((Object)(object)candidate == (Object)null)
			{
				continue;
			}
			Vector3 anchor;
			try
			{
				anchor = GetShelfSurfaceAnchor(candidate);
			}
			catch (System.Exception)
			{
				continue;
			}
			if (!TryScorePointAgainstRay(ray, maxDistance, anchor, out float score))
			{
				continue;
			}
			if (score < bestScore)
			{
				bestScore = score;
				slot = candidate;
			}
		}
		return (Object)(object)slot != (Object)null;
	}

	private static bool TryGetRackSlotNearRay(Ray ray, float maxDistance, out RackSlot slot)
	{
		slot = null;
		RackSlot[] slots = _rackSlotCache;
		if (slots == null || slots.Length == 0)
		{
			return false;
		}
		float bestScore = float.MaxValue;
		for (int i = 0; i < slots.Length; i++)
		{
			RackSlot candidate = slots[i];
			if ((Object)(object)candidate == (Object)null)
			{
				continue;
			}
			Vector3 anchor = CandidatePosition(candidate);
			if (!TryScorePointAgainstRay(ray, maxDistance, anchor, out float score))
			{
				continue;
			}
			if (score < bestScore)
			{
				bestScore = score;
				slot = candidate;
			}
		}
		return (Object)(object)slot != (Object)null;
	}

	private static bool TryScorePointAgainstRay(Ray ray, float maxDistance, Vector3 point, out float score)
	{
		score = float.MaxValue;
		Vector3 toPoint = point - ray.origin;
		float along = Vector3.Dot(toPoint, ray.direction);
		if (along < 0.35f || along > maxDistance)
		{
			return false;
		}
		Vector3 closestOnRay = ray.origin + ray.direction * along;
		float lateral = Vector3.Distance(closestOnRay, point);
		if (lateral > MaxRayLateralDistance)
		{
			return false;
		}
		score = lateral * 4f + along * 0.02f;
		return true;
	}

	private static void HandleDisplaySlotSwapKey(DisplaySlot slot)
	{
		if ((Object)(object)_selectedRackSlot != (Object)null)
		{
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap blocked: display shelves and box racks cannot be swapped with each other.");
			return;
		}
		if (NetworkShelfSync.InMultiplayer && !NetworkShelfSync.CanSwapInMultiplayer)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning(
				(object)("Shelf swap blocked: co-op handshake mismatch or missing (sps_hs="
					+ ShelfProductSwapperPlugin.PluginVersion + "). Match mods on all PCs."));
			return;
		}
		SlotSnapshot slotSnapshot = SlotSnapshot.From(slot);
		if (!slotSnapshot.HasProduct && !ShelfProductSwapperPlugin.AllowEmptySlots.Value)
		{
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap: selected slot is empty.");
			return;
		}
		if ((Object)(object)_selectedSlot == (Object)null)
		{
			_selectedSlot = slot;
			_selectedSnapshot = SlotSnapshot.From(slot);
			UpdateMarkers();
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Shelf swap source selected: " + Describe(slotSnapshot) + "."));
			return;
		}
		if ((Object)(object)_selectedSlot == (Object)(object)slot)
		{
			ClearSelection();
			UpdateMarkers();
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap selection cleared.");
			return;
		}
		SlotSnapshot slotSnapshot2 = SlotSnapshot.From(_selectedSlot);
		if (!CanSwap(_selectedSlot, slotSnapshot2, slot, slotSnapshot))
		{
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap blocked: selected products do not match the target furniture type.");
			return;
		}
		DisplaySlot source = _selectedSlot;
		if (!NetworkShelfSync.TryBeginDisplaySwap(source, slot))
		{
			return;
		}
		ExecuteDisplaySwap(source, slot);
	}

	private static void HandleRackSlotSwapKey(RackSlot slot)
	{
		if ((Object)(object)_selectedSlot != (Object)null)
		{
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap blocked: display shelves and box racks cannot be swapped with each other.");
			return;
		}
		if (NetworkShelfSync.InMultiplayer && !NetworkShelfSync.CanSwapInMultiplayer)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning(
				(object)("Box rack swap blocked: co-op handshake mismatch or missing (sps_hs="
					+ ShelfProductSwapperPlugin.PluginVersion + "). Match mods on all PCs."));
			return;
		}
		RackSlotSnapshot snapshot = RackSlotSnapshot.From(slot);
		if (!snapshot.HasBox && !ShelfProductSwapperPlugin.AllowEmptySlots.Value)
		{
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Box rack swap: selected slot is empty.");
			return;
		}
		if ((Object)(object)_selectedRackSlot == (Object)null)
		{
			_selectedRackSlot = slot;
			_selectedRackSnapshot = RackSlotSnapshot.From(slot);
			UpdateMarkers();
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Box rack swap source selected: " + Describe(snapshot) + "."));
			return;
		}
		if ((Object)(object)_selectedRackSlot == (Object)(object)slot)
		{
			ClearSelection();
			UpdateMarkers();
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Box rack swap selection cleared.");
			return;
		}
		RackSlotSnapshot snapshot2 = RackSlotSnapshot.From(_selectedRackSlot);
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Box rack swap labels: source label=" + snapshot2.HasLabel + "/" + snapshot2.HasLabelData + " target label=" + snapshot.HasLabel + "/" + snapshot.HasLabelData + "."));
		RackSlot source = _selectedRackSlot;
		if (!NetworkShelfSync.TryBeginRackSwap(source, slot))
		{
			return;
		}
		ExecuteRackSwap(source, slot);
	}

	internal static void CompletePendingDisplaySwap(DisplaySlot source, DisplaySlot target)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)target == (Object)null)
		{
			return;
		}

		ExecuteDisplaySwap(source, target);
	}

	internal static void CompletePendingRackSwap(RackSlot source, RackSlot target)
	{
		if ((Object)(object)source == (Object)null || (Object)(object)target == (Object)null)
		{
			return;
		}

		ExecuteRackSwap(source, target);
	}

	private static void ExecuteDisplaySwap(DisplaySlot source, DisplaySlot target)
	{
		SlotSnapshot beforeSource = SlotSnapshot.From(source);
		SlotSnapshot beforeTarget = SlotSnapshot.From(target);
		if (!SwapSlots(source, target))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Shelf swap failed: could not move products using the original shelf APIs.");
			ClearSelection();
			UpdateMarkers();
			return;
		}

		NetworkShelfSync.SyncDisplayAfterSwap(source, target);
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Shelf swap complete: " + Describe(beforeSource) + " <-> " + Describe(beforeTarget) + "."));
		ClearSelection();
		UpdateMarkers();
	}

	private static void ExecuteRackSwap(RackSlot source, RackSlot target)
	{
		RackSlotSnapshot beforeSource = RackSlotSnapshot.From(source);
		RackSlotSnapshot beforeTarget = RackSlotSnapshot.From(target);
		if (!SwapRackSlots(source, target))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Box rack swap failed: could not move boxes using the original rack APIs.");
			ClearSelection();
			UpdateMarkers();
			return;
		}

		NetworkShelfSync.SyncRackAfterSwap(source, target);
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Box rack swap complete: " + Describe(beforeSource) + " <-> " + Describe(beforeTarget) + "."));
		ClearSelection();
		UpdateMarkers();
	}

	private static void HandleToggleIncompatibleKey()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		KeyCode value = ShelfProductSwapperPlugin.ToggleIncompatibleKey.Value;
		if ((int)value != 0 && Input.GetKeyDown(value))
		{
			ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value = !ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value;
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Shelf swap incompatible display types: " + (ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value ? "ALLOWED" : "BLOCKED") + "."));
			UpdateMarkers();
			ShelfProductSwapperHints.NotifyStateChanged();
		}
	}

	private static bool TryGetDisplaySlotFromCollider(Collider collider, out DisplaySlot slot)
	{
		int id = ((Object)collider).GetInstanceID();
		if (DisplaySlotByCollider.TryGetValue(id, out slot))
		{
			if ((Object)(object)slot != (Object)null)
			{
				return true;
			}

			DisplaySlotByCollider.Remove(id);
		}

		slot = collider.GetComponentInParent<DisplaySlot>();
		if ((Object)(object)slot != (Object)null)
		{
			DisplaySlotByCollider[id] = slot;
			return true;
		}
		PriceTag priceTag = collider.GetComponentInParent<PriceTag>();
		if ((Object)(object)priceTag != (Object)null && (Object)(object)priceTag.DisplaySlot != (Object)null)
		{
			slot = priceTag.DisplaySlot;
			DisplaySlotByCollider[id] = slot;
			return true;
		}
		Label label = collider.GetComponentInParent<Label>();
		if ((Object)(object)label != (Object)null && (Object)(object)label.DisplaySlot != (Object)null)
		{
			slot = label.DisplaySlot;
			DisplaySlotByCollider[id] = slot;
			return true;
		}
		Product product = collider.GetComponentInParent<Product>();
		if ((Object)(object)product != (Object)null && TryFindDisplaySlotForProduct(product, out slot))
		{
			DisplaySlotByCollider[id] = slot;
			return true;
		}
		return false;
	}

	private static bool TryFindDisplaySlotForProduct(Product product, out DisplaySlot slot)
	{
		slot = null;
		RefreshSlotCaches();
		DisplaySlot[] slots = _displaySlotCache;
		if (slots == null)
		{
			return false;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			DisplaySlot candidate = slots[i];
			if ((Object)(object)candidate == (Object)null || candidate.m_Products == null)
			{
				continue;
			}
			for (int j = 0; j < candidate.m_Products.Count; j++)
			{
				if ((Object)(object)candidate.m_Products[j] == (Object)(object)product)
				{
					slot = candidate;
					return true;
				}
			}
		}
		return false;
	}

	private static bool TryGetRackSlotFromCollider(Collider collider, out RackSlot slot)
	{
		int id = ((Object)collider).GetInstanceID();
		if (RackSlotByCollider.TryGetValue(id, out slot))
		{
			if ((Object)(object)slot != (Object)null)
			{
				return true;
			}

			RackSlotByCollider.Remove(id);
		}

		slot = ((Component)collider).GetComponentInParent<RackSlot>();
		if ((Object)(object)slot != (Object)null)
		{
			RackSlotByCollider[id] = slot;
			return true;
		}
		Label componentInParent = ((Component)collider).GetComponentInParent<Label>();
		if ((Object)(object)componentInParent != (Object)null && (Object)(object)componentInParent.RackSlot != (Object)null)
		{
			slot = componentInParent.RackSlot;
			RackSlotByCollider[id] = slot;
			return true;
		}
		Box componentInParent2 = ((Component)collider).GetComponentInParent<Box>();
		if ((Object)(object)componentInParent2 != (Object)null)
		{
			slot = ((Component)componentInParent2).GetComponentInParent<RackSlot>();
			if ((Object)(object)slot != (Object)null)
			{
				RackSlotByCollider[id] = slot;
				return true;
			}
			if (TryFindRackSlotForBox(componentInParent2, out slot))
			{
				RackSlotByCollider[id] = slot;
				return true;
			}
		}
		return false;
	}

	private static bool TryFindRackSlotForBox(Box box, out RackSlot slot)
	{
		slot = null;
		RefreshSlotCaches();
		RackSlot[] array = _rackSlotCache;
		if (array == null)
		{
			return false;
		}
		foreach (RackSlot val in array)
		{
			if ((Object)(object)val == (Object)null || val.Boxes == null)
			{
				continue;
			}
			for (int j = 0; j < val.Boxes.Count; j++)
			{
				if ((Object)(object)val.Boxes[j] == (Object)(object)box)
				{
					slot = val;
					return true;
				}
			}
		}
		return false;
	}

	private static Vector3 CandidatePosition(DisplaySlot slot)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Transform priceTagTransform = slot.PriceTagTransform;
		if ((Object)(object)priceTagTransform != (Object)null)
		{
			return priceTagTransform.position;
		}
		return slot.InteractionPosition;
	}

	private static Vector3 CandidatePosition(RackSlot slot)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (TryGetRackSlotBounds(slot, out var bounds))
		{
			return bounds.center;
		}
		return slot.InteractionPosition;
	}

	private static bool TryGetRackSlotBounds(RackSlot slot, out Bounds bounds)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)slot != (Object)null && slot.HasBox && slot.Boxes != null)
		{
			bool flag = false;
			bounds = new Bounds(slot.InteractionPosition, Vector3.zero);
			for (int i = 0; i < slot.Boxes.Count; i++)
			{
				Box val = slot.Boxes[i];
				if (!((Object)(object)val == (Object)null))
				{
					Bounds bounds2 = val.Bounds;
					if (!flag)
					{
						bounds = bounds2;
						flag = true;
					}
					else
					{
						bounds.Encapsulate(bounds2);
					}
				}
			}
			if (flag)
			{
				return true;
			}
		}
		Highlightable val2 = (((Object)(object)slot != (Object)null) ? slot.Highlightable : null);
		if ((Object)(object)val2 != (Object)null && val2.RenderersToHighlight != null)
		{
			bool flag2 = false;
			bounds = new Bounds(slot.InteractionPosition, Vector3.zero);
			for (int j = 0; j < val2.RenderersToHighlight.Count; j++)
			{
				Renderer val3 = val2.RenderersToHighlight[j];
				if (!((Object)(object)val3 == (Object)null) && val3.enabled)
				{
					if (!flag2)
					{
						bounds = val3.bounds;
						flag2 = true;
					}
					else
					{
						bounds.Encapsulate(val3.bounds);
					}
				}
			}
			if (flag2)
			{
				return true;
			}
		}
		Label val4 = (((Object)(object)slot != (Object)null) ? slot.Label : null);
		if ((Object)(object)val4 != (Object)null && (Object)(object)val4.m_Renderer != (Object)null && ((Renderer)val4.m_Renderer).enabled)
		{
			bounds = ((Renderer)val4.m_Renderer).bounds;
			return true;
		}
		bounds = default(Bounds);
		return false;
	}

	private static bool SwapSlots(DisplaySlot firstSlot, DisplaySlot secondSlot)
	{
		SlotSnapshot first = SlotSnapshot.From(firstSlot);
		SlotSnapshot second = SlotSnapshot.From(secondSlot);
		SlotProducts products = TakeAllProducts(firstSlot);
		SlotProducts products2 = TakeAllProducts(secondSlot);
		PrepareSlotForIncomingProducts(firstSlot, products2);
		PrepareSlotForIncomingProducts(secondSlot, products);
		AddProducts(firstSlot, products2);
		AddProducts(secondSlot, products);
		ApplyEmptyLabelRules(firstSlot, first, secondSlot, second);
		RefreshSlot(firstSlot);
		RefreshSlot(secondSlot);
		if (SlotSnapshot.From(firstSlot).Count == products2.Count)
		{
			return SlotSnapshot.From(secondSlot).Count == products.Count;
		}
		return false;
	}

	private static void PrepareSlotForIncomingProducts(DisplaySlot slot, SlotProducts incoming)
	{
		if ((Object)(object)slot == (Object)null || incoming.Count <= 0)
		{
			return;
		}
		if (Mathf.Max(0, slot.ProductCount) > 0)
		{
			return;
		}
		slot.ClearProductData();
		Label label = slot.Label;
		if ((Object)(object)label != (Object)null)
		{
			label.ClearLabel();
		}
	}

	private static void ApplyEmptyLabelRules(DisplaySlot firstSlot, SlotSnapshot first, DisplaySlot secondSlot, SlotSnapshot second)
	{
		if (!first.HasProduct && !second.HasProduct)
		{
			SwapEmptyLabels(firstSlot, first, secondSlot, second);
			EnsureSlotPhysicallyEmpty(firstSlot);
			EnsureSlotPhysicallyEmpty(secondSlot);
			return;
		}
		ApplyMovedIntoEmptySlotRules(firstSlot, first, secondSlot, second);
		ApplyMovedIntoEmptySlotRules(secondSlot, second, firstSlot, first);
	}

	private static void SwapEmptyLabels(DisplaySlot firstSlot, SlotSnapshot first, DisplaySlot secondSlot, SlotSnapshot second)
	{
		ItemQuantity firstData = firstSlot.Data;
		ItemQuantity secondData = secondSlot.Data;
		firstSlot.Data = secondData;
		secondSlot.Data = firstData;
		if (second.HasLabel)
		{
			RefreshEmptyLabelVisual(firstSlot, second.LabelProductId);
		}
		else
		{
			ClearSlotLabelVisual(firstSlot);
		}
		if (first.HasLabel)
		{
			RefreshEmptyLabelVisual(secondSlot, first.LabelProductId);
		}
		else
		{
			ClearSlotLabelVisual(secondSlot);
		}
		ForceEmptyLabelCount(firstSlot);
		ForceEmptyLabelCount(secondSlot);
	}

	private static void ApplyMovedIntoEmptySlotRules(DisplaySlot sourceSlot, SlotSnapshot source, DisplaySlot targetSlot, SlotSnapshot target)
	{
		if (!source.HasProduct || target.HasProduct)
		{
			return;
		}
		if (target.HasLabel)
		{
			SetEmptySlotLabel(sourceSlot, target.LabelProductId, target.LabelPrice);
		}
		else
		{
			ClearSlotLabel(sourceSlot);
		}
		EnsureSlotPhysicallyEmpty(sourceSlot);
		SyncFilledSlotLabel(targetSlot, source);
	}

	private static void SetEmptySlotLabel(DisplaySlot slot, int productId, float price)
	{
		if ((Object)(object)slot == (Object)null || productId < 0)
		{
			return;
		}
		ItemQuantity data = CreateEmptyLabelData(productId, price);
		slot.Data = data;
		ForceEmptyLabelCount(slot);
		RefreshEmptyLabelVisual(slot, productId);
	}

	private static ItemQuantity CreateEmptyLabelData(int productId, float price)
	{
		Il2CppSystem.Collections.Generic.Dictionary<int, int> products = new Il2CppSystem.Collections.Generic.Dictionary<int, int>();
		products.Add(productId, 0);
		Il2CppSystem.Collections.Generic.Dictionary<int, float> prices = new Il2CppSystem.Collections.Generic.Dictionary<int, float>();
		prices.Add(productId, price);
		ItemQuantity data = new ItemQuantity(products, prices);
		data.FirstItemCount = 0;
		return data;
	}

	private static void ForceEmptyLabelCount(DisplaySlot slot)
	{
		if ((Object)(object)slot == (Object)null || slot.Data == null)
		{
			return;
		}
		if (slot.Data.FirstItemCount != 0)
		{
			slot.Data.FirstItemCount = 0;
		}
	}

	private static void RefreshEmptyLabelVisual(DisplaySlot slot, int productId)
	{
		Label label = slot.Label;
		if ((Object)(object)label == (Object)null || productId < 0)
		{
			return;
		}
		label.SetProductIcon(productId);
		label.ProductCount = 0;
	}

	private static void ClearSlotLabelVisual(DisplaySlot slot)
	{
		Label label = slot.Label;
		if ((Object)(object)label != (Object)null)
		{
			label.ClearLabel();
		}
	}

	private static void EnsureSlotPhysicallyEmpty(DisplaySlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return;
		}
		ForceEmptyLabelCount(slot);
		int guard = 12;
		while (guard-- > 0)
		{
			if (Mathf.Max(0, slot.ProductCount) <= 0)
			{
				break;
			}
			Product product = slot.TakeProductFromDisplay();
			if ((Object)(object)product == (Object)null)
			{
				break;
			}
			DestroyTakenProduct(product);
		}
		if (slot.m_Products == null)
		{
			return;
		}
		for (int i = slot.m_Products.Count - 1; i >= 0; i--)
		{
			Product leftover = slot.m_Products[i];
			if ((Object)(object)leftover != (Object)null)
			{
				DestroyTakenProduct(leftover);
			}
		}
		slot.m_Products.Clear();
	}

	private static void DestroyTakenProduct(Product product)
	{
		if ((Object)(object)product == (Object)null)
		{
			return;
		}
		GameObject go = product.gameObject;
		if ((Object)(object)go != (Object)null)
		{
			Object.Destroy(go);
		}
	}

	private static void SyncFilledSlotLabel(DisplaySlot slot, SlotSnapshot movedFrom)
	{
		if ((Object)(object)slot == (Object)null || !movedFrom.HasProduct)
		{
			return;
		}
		Label label = slot.Label;
		if ((Object)(object)label == (Object)null)
		{
			return;
		}
		int productId = movedFrom.ProductId >= 0 ? movedFrom.ProductId : slot.ProductID;
		if (productId < 0)
		{
			return;
		}
		label.SetProductIcon(productId);
		label.ProductCount = Mathf.Max(0, slot.ProductCount);
	}

	private static void ClearSlotLabel(DisplaySlot slot)
	{
		if (!((Object)(object)slot == (Object)null))
		{
			slot.ClearProductData();
			ClearSlotLabelVisual(slot);
		}
	}

	private static bool SwapRackSlots(RackSlot firstSlot, RackSlot secondSlot)
	{
		RackSlotSnapshot first = RackSlotSnapshot.From(firstSlot);
		RackSlotSnapshot second = RackSlotSnapshot.From(secondSlot);
		RackSlotBoxes boxes = TakeAllBoxes(firstSlot);
		RackSlotBoxes boxes2 = TakeAllBoxes(secondSlot);
		AddBoxes(firstSlot, boxes2);
		AddBoxes(secondSlot, boxes);
		ApplyRackEmptyLabelRules(firstSlot, first, secondSlot, second);
		RequestRackSlotMaskUpdate(firstSlot);
		RequestRackSlotMaskUpdate(secondSlot);
		if (RackSlotSnapshot.From(firstSlot).BoxCount == boxes2.Count)
		{
			return RackSlotSnapshot.From(secondSlot).BoxCount == boxes.Count;
		}
		return false;
	}

	private static RackSlotBoxes TakeAllBoxes(RackSlot slot)
	{
		RackSlotBoxes result = new RackSlotBoxes();
		result.BoxId = slot.CurrentBoxID;
		result.ProductId = ((slot.Data != null) ? slot.Data.ProductID : (-1));
		int num = Mathf.Max(0, (slot.Boxes != null) ? slot.Boxes.Count : 0) + 8;
		while (slot.HasBox && num-- > 0)
		{
			Box val = slot.TakeBoxFromRack();
			if ((Object)(object)val == (Object)null)
			{
				break;
			}
			result.Boxes.Add(val);
		}
		result.Count = result.Boxes.Count;
		return result;
	}

	private static void AddBoxes(RackSlot slot, RackSlotBoxes boxes)
	{
		if (boxes.BoxId < 0 || boxes.Boxes.Count == 0)
		{
			return;
		}
		for (int i = 0; i < boxes.Boxes.Count; i++)
		{
			Box val = boxes.Boxes[i];
			if ((Object)(object)val != (Object)null)
			{
				slot.AddBox(boxes.BoxId, val, true);
			}
		}
	}

	private static void ApplyRackEmptyLabelRules(RackSlot firstSlot, RackSlotSnapshot first, RackSlot secondSlot, RackSlotSnapshot second)
	{
		if (!first.HasBox && !second.HasBox)
		{
			ApplyRackLabelSnapshot(firstSlot, second);
			ApplyRackLabelSnapshot(secondSlot, first);
		}
		else
		{
			ApplyRackMovedIntoEmptySlotRules(firstSlot, first, secondSlot, second);
			ApplyRackMovedIntoEmptySlotRules(secondSlot, second, firstSlot, first);
		}
	}

	private static void ApplyRackMovedIntoEmptySlotRules(RackSlot sourceSlot, RackSlotSnapshot source, RackSlot targetSlot, RackSlotSnapshot target)
	{
		if (source.HasBox && !target.HasBox)
		{
			if (target.HasLabel)
			{
				ApplyRackLabelSnapshot(sourceSlot, target);
				ApplyRackLabelSnapshot(targetSlot, source);
			}
			else
			{
				ClearRackSlotLabel(sourceSlot);
			}
		}
	}

	private static void ApplyRackLabelSnapshot(RackSlot slot, RackSlotSnapshot snapshot)
	{
		if (snapshot.HasLabelData)
		{
			SetRackSlotLabel(slot, snapshot.LabelProductId, snapshot.LabelBoxId);
		}
		else if (snapshot.HasLabel)
		{
			if (!RackSlotSnapshot.From(slot).HasLabel)
			{
				slot.SetLabel();
				slot.RefreshLabel();
			}
		}
		else
		{
			ClearRackSlotLabel(slot);
		}
	}

	private static void SetRackSlotLabel(RackSlot slot, int productId, int boxId)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		if (!((Object)(object)slot == (Object)null) && productId >= 0)
		{
			if (boxId < 0)
			{
				boxId = 0;
			}
			if (slot.Data == null)
			{
				slot.Data = new RackSlotData();
			}
			slot.Data.Setup(productId, boxId);
			slot.SetLabel();
			slot.RefreshLabel();
		}
	}

	private static void ClearRackSlotLabel(RackSlot slot)
	{
		if (!((Object)(object)slot == (Object)null))
		{
			slot.ClearLabel();
		}
	}

	private static void RequestRackSlotMaskUpdate(RackSlot slot)
	{
		if (!((Object)(object)slot == (Object)null))
		{
			slot.RequestLabelMaskUpdate();
		}
	}

	private static bool TryGetRackLabelData(RackSlot slot, out int productId, out int boxId)
	{
		productId = -1;
		boxId = -1;
		if ((Object)(object)slot == (Object)null)
		{
			return false;
		}
		if (slot.Data != null && slot.Data.ProductID >= 0)
		{
			productId = slot.Data.ProductID;
			boxId = ((slot.Data.BoxID >= 0) ? slot.Data.BoxID : slot.CurrentBoxID);
			if (boxId < 0)
			{
				boxId = 0;
			}
			return productId >= 0;
		}
		if (slot.HasBox)
		{
			productId = GetProductIdFromRackSlot(slot);
			boxId = slot.CurrentBoxID;
			if (productId >= 0)
			{
				return boxId >= 0;
			}
			return false;
		}
		Label label = slot.Label;
		if ((Object)(object)label != (Object)null && label.Enabled)
		{
			productId = ((slot.CurrentBoxID >= 0) ? GetProductIdFromRackSlot(slot) : (-1));
			boxId = slot.CurrentBoxID;
			if (productId >= 0)
			{
				return boxId >= 0;
			}
			return false;
		}
		return false;
	}

	private static bool HasAnyRackLabel(RackSlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return false;
		}
		if (slot.HasLabel)
		{
			return true;
		}
		Label label = slot.Label;
		if ((Object)(object)label == (Object)null)
		{
			return false;
		}
		if (label.Enabled)
		{
			return true;
		}
		if ((Object)(object)label.m_Renderer != (Object)null)
		{
			return ((Renderer)label.m_Renderer).enabled;
		}
		return false;
	}

	private static int GetProductIdFromRackSlot(RackSlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return -1;
		}
		Box val = slot.PeakBoxFromRack();
		if ((Object)(object)val != (Object)null && val.Data != null)
		{
			return val.Data.ProductID;
		}
		if (slot.Data == null)
		{
			return -1;
		}
		return slot.Data.ProductID;
	}

	private static SlotProducts TakeAllProducts(DisplaySlot slot)
	{
		SlotProducts result = new SlotProducts();
		int expected = Mathf.Max(0, slot.ProductCount);
		if ((Object)(object)slot == (Object)null || expected <= 0)
		{
			result.ProductId = -1;
			result.Count = 0;
			return result;
		}
		result.ProductId = slot.ProductID;
		int guard = expected + 2;
		while (result.Products.Count < expected && guard-- > 0)
		{
			if (!slot.HasProduct || slot.ProductCount <= 0)
			{
				break;
			}
			Product val = slot.TakeProductFromDisplay();
			if ((Object)(object)val == (Object)null)
			{
				break;
			}
			result.Products.Add(val);
		}
		result.Count = result.Products.Count;
		return result;
	}

	private static void AddProducts(DisplaySlot slot, SlotProducts products)
	{
		if (products.ProductId < 0 || products.Products.Count == 0)
		{
			return;
		}
		for (int i = 0; i < products.Products.Count; i++)
		{
			Product val = products.Products[i];
			if ((Object)(object)val != (Object)null)
			{
				slot.AddProduct(products.ProductId, val);
			}
		}
	}

	private static void HandleToggleModKey()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		KeyCode value = ShelfProductSwapperPlugin.ToggleModKey.Value;
		if ((int)value != 0 && Input.GetKeyDown(value))
		{
			ShelfProductSwapperPlugin.Enabled.Value = !ShelfProductSwapperPlugin.Enabled.Value;
			if (!ShelfProductSwapperPlugin.Enabled.Value)
			{
				ClearSelection();
				HideMarkers();
			}
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)("Shelf Product Swapper: " + (ShelfProductSwapperPlugin.Enabled.Value ? "ON" : "OFF") + "."));
			ShelfProductSwapperHints.NotifyStateChanged();
		}
	}

	private static void RefreshSlot(DisplaySlot slot)
	{
		if (!((Object)(object)slot == (Object)null))
		{
			slot.RequestLabelMaskUpdate();
		}
	}

	internal static void PrepareRemoteDisplaySlot(DisplaySlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return;
		}

		slot.ResetSlot();
	}

	internal static void ApplyRemoteDisplaySlot(DisplaySlot slot, int productId, int count, int labelProductId, float labelPrice)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return;
		}

		slot.ResetSlot();
		count = Mathf.Max(0, count);
		if (count > 0 && productId >= 0)
		{
			slot.SpawnProduct(productId, count);
			TrySetFilledSlotPrice(slot, productId, labelPrice);
			slot.SetLabel();
			slot.SetPriceTag();
		}
		else if (labelProductId >= 0)
		{
			SetEmptySlotLabel(slot, labelProductId, labelPrice);
			slot.SetLabel();
		}

		RefreshSlot(slot);
	}

	private static void TrySetFilledSlotPrice(DisplaySlot slot, int productId, float labelPrice)
	{
		if ((Object)(object)slot == (Object)null || productId < 0 || slot.Data == null)
		{
			return;
		}

		Il2CppSystem.Collections.Generic.Dictionary<int, float> prices = slot.Data.ProductPrice;
		if (prices == null)
		{
			prices = new Il2CppSystem.Collections.Generic.Dictionary<int, float>();
			slot.Data.ProductPrice = prices;
		}

		if (prices.ContainsKey(productId))
		{
			prices[productId] = labelPrice;
		}
		else
		{
			prices.Add(productId, labelPrice);
		}
	}

	internal static void ApplyRemoteRackSlot(
		RackSlot slot,
		int boxId,
		int labelProductId,
		System.Collections.Generic.List<RemoteBoxEntry> boxes)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return;
		}

		int guard = Mathf.Max(8, (slot.Boxes != null ? slot.Boxes.Count : 0) + 8);
		while (slot.HasBox && guard-- > 0)
		{
			Box box = slot.TakeBoxFromRack();
			if ((Object)(object)box == (Object)null)
			{
				break;
			}

			Object.Destroy(((Component)box).gameObject);
		}

		int boxCount = boxes != null ? boxes.Count : 0;
		if (boxCount > 0 && boxId >= 0)
		{
			BoxGenerator generator = Object.FindObjectOfType<BoxGenerator>();
			Rack rack = slot.OwnRack;
			if ((Object)(object)generator != (Object)null && (Object)(object)rack != (Object)null)
			{
				for (int i = 0; i < boxCount; i++)
				{
					RemoteBoxEntry entry = boxes[i];
					int productId = entry.ProductId;
					int productCount = Mathf.Max(0, entry.ProductCount);
					if (productId < 0)
					{
						continue;
					}

					BoxData data = new BoxData
					{
						ProductID = productId,
						ProductCount = productCount
					};
					Box spawned = generator.SpawnBoxInRack(
						((Component)slot).transform.position,
						((Component)slot).transform.rotation,
						data,
						((Component)slot).transform,
						rack,
						slot);
					if ((Object)(object)spawned != (Object)null)
					{
						slot.AddBox(boxId, spawned, true);
					}
				}
			}
		}

		if (labelProductId >= 0)
		{
			if (slot.Data == null)
			{
				slot.Data = new RackSlotData();
			}

			slot.Data.ProductID = labelProductId;
			if (boxId >= 0)
			{
				slot.Data.BoxID = boxId;
			}

			slot.SetLabel();
			slot.RefreshLabel();
		}
		else
		{
			slot.ClearLabel();
		}

		RequestRackSlotMaskUpdate(slot);
	}

	private static void ClearSelection()
	{
		_selectedSlot = null;
		_selectedSnapshot = default(SlotSnapshot);
		_selectedRackSlot = null;
		_selectedRackSnapshot = default(RackSlotSnapshot);
	}

	private static void UpdateMarkers()
	{
		if (ShelfProductSwapperPlugin.ShowSelectionMarkers == null || !ShelfProductSwapperPlugin.ShowSelectionMarkers.Value)
		{
			HideMarkers();
			return;
		}
		bool hoverChanged = (Object)(object)_hoverSlot != (Object)(object)_lastHoverSlot
			|| (Object)(object)_hoverRackSlot != (Object)(object)_lastHoverRackSlot;
		bool selectedChanged = (Object)(object)_selectedSlot != (Object)(object)_lastSelectedSlot
			|| (Object)(object)_selectedRackSlot != (Object)(object)_lastSelectedRackSlot;
		if (!hoverChanged && !selectedChanged && (Object)(object)_markerRoot != (Object)null && _markerRoot.activeSelf)
		{
			return;
		}

		bool compatible = IsHoverCompatible();
		_lastHoverSlot = _hoverSlot;
		_lastHoverRackSlot = _hoverRackSlot;
		_lastSelectedSlot = _selectedSlot;
		_lastSelectedRackSlot = _selectedRackSlot;
		_lastHoverCompatible = compatible;
		if (hoverChanged)
		{
			_markerRectSlot = null;
		}
		EnsureMarkers();
		Color color = compatible ? new Color(0.1f, 0.9f, 1f, 0.9f) : new Color(1f, 0.1f, 0.1f, 0.95f);
		if ((Object)(object)_hoverSlot != (Object)null)
		{
			UpdateMarker(_hoverMarker, _hoverSlot, color, 0.04f);
		}
		else
		{
			UpdateMarker(_hoverMarker, _hoverRackSlot, color, 0.04f, 0.36f);
		}
		if ((Object)(object)_selectedSlot != (Object)null)
		{
			UpdateMarker(_selectedMarker, _selectedSlot, new Color(1f, 0.85f, 0.05f, 0.95f), 0.055f);
		}
		else
		{
			UpdateMarker(_selectedMarker, _selectedRackSlot, new Color(1f, 0.85f, 0.05f, 0.95f), 0.055f, 0.48f);
		}
		if (ShelfProductSwapperPlugin.UseNativeOutlines != null && ShelfProductSwapperPlugin.UseNativeOutlines.Value)
		{
			SetNativeOutlines(GetHoverHighlightable(), GetSelectedHighlightable());
		}
		else if ((Object)(object)_lastHoverHighlightable != (Object)null || (Object)(object)_lastSelectedHighlightable != (Object)null)
		{
			SetNativeOutlines(null, null);
		}
	}

	private static bool IsHoverCompatible()
	{
		if (((Object)(object)_selectedSlot != (Object)null && (Object)(object)_hoverRackSlot != (Object)null) || ((Object)(object)_selectedRackSlot != (Object)null && (Object)(object)_hoverSlot != (Object)null))
		{
			return false;
		}
		if ((Object)(object)_hoverSlot != (Object)null && (Object)(object)_selectedSlot != (Object)null && (Object)(object)_hoverSlot != (Object)(object)_selectedSlot)
		{
			return CanSwap(_selectedSlot, SlotSnapshot.From(_selectedSlot), _hoverSlot, SlotSnapshot.From(_hoverSlot));
		}
		if ((Object)(object)_hoverRackSlot != (Object)null && (Object)(object)_selectedRackSlot != (Object)null)
		{
			_ = (Object)(object)_hoverRackSlot != (Object)(object)_selectedRackSlot;
			return true;
		}
		return true;
	}

	private static void HideMarkers()
	{
		if ((Object)(object)_markerRoot != (Object)null)
		{
			_markerRoot.SetActive(false);
		}
		_lastHoverSlot = null;
		_lastHoverRackSlot = null;
		_lastSelectedSlot = null;
		_lastSelectedRackSlot = null;
		_markerRectSlot = null;
		SetNativeOutlines(null, null);
	}

	private static Highlightable GetHoverHighlightable()
	{
		if ((Object)(object)_hoverSlot != (Object)null)
		{
			return _hoverSlot.ActiveHighlightable;
		}
		if (!((Object)(object)_hoverRackSlot != (Object)null))
		{
			return null;
		}
		return _hoverRackSlot.Highlightable;
	}

	private static Highlightable GetSelectedHighlightable()
	{
		if ((Object)(object)_selectedSlot != (Object)null)
		{
			return _selectedSlot.ActiveHighlightable;
		}
		if (!((Object)(object)_selectedRackSlot != (Object)null))
		{
			return null;
		}
		return _selectedRackSlot.Highlightable;
	}

	private static void SetNativeOutlines(Highlightable hover, Highlightable selected)
	{
		if ((Object)(object)_lastHoverHighlightable != (Object)null && (Object)(object)_lastHoverHighlightable != (Object)(object)hover && (Object)(object)_lastHoverHighlightable != (Object)(object)selected)
		{
			_lastHoverHighlightable.Highlighted = false;
		}
		if ((Object)(object)_lastSelectedHighlightable != (Object)null && (Object)(object)_lastSelectedHighlightable != (Object)(object)selected && (Object)(object)_lastSelectedHighlightable != (Object)(object)hover)
		{
			_lastSelectedHighlightable.Highlighted = false;
		}
		if ((Object)(object)hover != (Object)null)
		{
			hover.Highlighted = true;
		}
		if ((Object)(object)selected != (Object)null)
		{
			selected.Highlighted = true;
		}
		_lastHoverHighlightable = hover;
		_lastSelectedHighlightable = selected;
	}

	private static void EnsureMarkers()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		if ((Object)(object)_markerRoot != (Object)null)
		{
			if (!_markerRoot.activeSelf)
			{
				_markerRoot.SetActive(true);
			}
		}
		else
		{
			_markerRoot = new GameObject("ShelfProductSwapper_Markers");
			Object.DontDestroyOnLoad(_markerRoot);
			Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Hidden/Internal-Colored");
			_markerMaterial = shader != null ? new Material(shader) : null;
			if (_markerMaterial != null)
			{
				_markerMaterial.color = new Color(0.1f, 0.95f, 1f, 0.95f);
			}
			_hoverMarker = CreateMarker("HoverMarker");
			_selectedMarker = CreateMarker("SelectedMarker");
		}
	}

	private static LineRenderer CreateMarker(string name)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject(name);
		val.transform.SetParent(_markerRoot.transform, false);
		LineRenderer obj = val.AddComponent<LineRenderer>();
		obj.useWorldSpace = true;
		obj.loop = true;
		obj.positionCount = 4;
		obj.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		obj.receiveShadows = false;
		obj.allowOcclusionWhenDynamic = false;
		obj.material = _markerMaterial;
		obj.gameObject.SetActive(false);
		return obj;
	}

	private static void UpdateMarker(LineRenderer marker, DisplaySlot slot, Color color, float width)
	{
		if ((Object)(object)marker == (Object)null)
		{
			return;
		}
		if ((Object)(object)slot == (Object)null)
		{
			marker.gameObject.SetActive(false);
			return;
		}
		try
		{
			if (!TryGetDisplaySlotMarkerRect(slot, out Vector3 center, out Vector3 right, out Vector3 intoShelf, out float halfW, out float halfD))
			{
				marker.gameObject.SetActive(false);
				return;
			}
			marker.startWidth = width;
			marker.endWidth = width;
			marker.startColor = color;
			marker.endColor = color;
			marker.SetPosition(0, center - right * halfW - intoShelf * halfD);
			marker.SetPosition(1, center + right * halfW - intoShelf * halfD);
			marker.SetPosition(2, center + right * halfW + intoShelf * halfD);
			marker.SetPosition(3, center - right * halfW + intoShelf * halfD);
			marker.gameObject.SetActive(true);
		}
		catch (System.Exception)
		{
			marker.gameObject.SetActive(false);
		}
	}

	private static bool TryGetDisplaySlotMarkerRect(DisplaySlot slot, out Vector3 center, out Vector3 right, out Vector3 intoShelf, out float halfW, out float halfD)
	{
		center = default;
		right = Vector3.right;
		intoShelf = Vector3.forward;
		halfW = 0.5f;
		halfD = 0.32f;
		if ((Object)(object)slot == (Object)null)
		{
			return false;
		}
		if ((Object)(object)_markerRectSlot == (Object)(object)slot)
		{
			center = _markerRectCenter;
			right = _markerRectRight;
			intoShelf = _markerRectForward;
			halfW = _markerRectHalfW;
			halfD = _markerRectHalfD;
			return true;
		}
		BuildDisplaySlotMarkerRect(slot, out center, out right, out intoShelf, out halfW, out halfD);
		_markerRectSlot = slot;
		_markerRectCenter = center;
		_markerRectRight = right;
		_markerRectForward = intoShelf;
		_markerRectHalfW = halfW;
		_markerRectHalfD = halfD;
		return true;
	}

	private static void BuildDisplaySlotMarkerRect(DisplaySlot slot, out Vector3 center, out Vector3 right, out Vector3 intoShelf, out float halfW, out float halfD)
	{
		GetShelfAxes(slot, out right, out intoShelf);
		Vector2 margin = slot.m_InteractionPositionMargin;
		halfW = Mathf.Clamp(Mathf.Abs(margin.x), 0.35f, 1.35f);
		halfD = Mathf.Clamp(Mathf.Abs(margin.y) * 0.7f, 0.22f, 0.55f);
		Vector3 front = GetSlotFrontAnchor(slot, intoShelf, halfD);
		center = front + intoShelf * halfD;
		center.y = front.y + 0.1f;
	}

	private static void GetShelfAxes(DisplaySlot slot, out Vector3 right, out Vector3 intoShelf)
	{
		intoShelf = FlattenAxis(slot.InteractionPositionForward);
		if (intoShelf.sqrMagnitude < 0.0001f)
		{
			intoShelf = FlattenAxis(slot.InteractionRotation * Vector3.forward);
		}
		Transform tag = slot.PriceTagTransform;
		Vector3 pad = slot.InteractionPosition;
		if ((Object)(object)tag != (Object)null)
		{
			Vector3 padToTag = FlattenAxis(tag.position - pad);
			if (padToTag.sqrMagnitude > 0.0001f)
			{
				if (intoShelf.sqrMagnitude < 0.0001f || Vector3.Dot(intoShelf, padToTag) < 0f)
				{
					intoShelf = padToTag;
				}
			}
			else if (intoShelf.sqrMagnitude < 0.0001f)
			{
				intoShelf = FlattenAxis(-tag.forward);
			}
		}
		if (intoShelf.sqrMagnitude < 0.0001f)
		{
			intoShelf = Vector3.forward;
		}
		right = Vector3.Cross(intoShelf, Vector3.up);
		if (right.sqrMagnitude < 0.0001f)
		{
			right = FlattenAxis(slot.InteractionRotation * Vector3.right);
		}
		if (right.sqrMagnitude < 0.0001f)
		{
			right = Vector3.right;
		}
		right.Normalize();
		intoShelf = Vector3.Cross(Vector3.up, right).normalized;
		if ((Object)(object)tag != (Object)null)
		{
			Vector3 padToTag = FlattenAxis(tag.position - pad);
			if (padToTag.sqrMagnitude > 0.0001f && Vector3.Dot(intoShelf, padToTag) < 0f)
			{
				intoShelf = -intoShelf;
				right = -right;
			}
		}
	}

	private static Vector3 GetSlotFrontAnchor(DisplaySlot slot, Vector3 intoShelf, float halfD)
	{
		Transform tag = slot.PriceTagTransform;
		if ((Object)(object)tag != (Object)null)
		{
			return tag.position;
		}
		Label label = slot.Label;
		if ((Object)(object)label != (Object)null)
		{
			return label.transform.position;
		}
		Vector2 margin = slot.m_InteractionPositionMargin;
		return slot.InteractionPosition + intoShelf * Mathf.Abs(margin.y) + Vector3.up * 1.1f - intoShelf * halfD;
	}

	private static Vector3 GetShelfSurfaceAnchor(DisplaySlot slot)
	{
		Transform tag = slot.PriceTagTransform;
		if ((Object)(object)tag != (Object)null)
		{
			Vector3 intoShelf = FlattenAxis(slot.InteractionPositionForward);
			if (intoShelf.sqrMagnitude < 0.0001f)
			{
				intoShelf = FlattenAxis(slot.InteractionRotation * Vector3.forward);
			}
			if (intoShelf.sqrMagnitude < 0.0001f)
			{
				intoShelf = FlattenAxis(-tag.forward);
			}
			if (intoShelf.sqrMagnitude < 0.0001f)
			{
				intoShelf = Vector3.forward;
			}
			return tag.position + intoShelf * 0.28f + Vector3.up * 0.08f;
		}
		Label label = slot.Label;
		if ((Object)(object)label != (Object)null)
		{
			return label.transform.position + Vector3.up * 0.12f;
		}
		return slot.InteractionPosition + Vector3.up * 1.1f;
	}

	private static Vector3 FlattenAxis(Vector3 axis)
	{
		axis.y = 0f;
		return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.zero;
	}

	private static void UpdateMarker(LineRenderer marker, RackSlot slot, Color color, float width, float size)
	{
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)marker == (Object)null)
		{
			return;
		}
		if ((Object)(object)slot == (Object)null)
		{
			((Component)marker).gameObject.SetActive(false);
			return;
		}
		float num = size;
		float num2 = size;
		Vector3 val;
		Bounds bounds2;
		if (slot.HasBox && TryGetRackSlotBounds(slot, out var bounds))
		{
			val = bounds.center;
			val.y = bounds.min.y + 0.04f;
			num = Mathf.Max(size, bounds.extents.x + 0.04f);
			num2 = Mathf.Max(size, bounds.extents.z + 0.04f);
		}
		else if (TryGetRackEmptyMarkerBounds(slot, out bounds2))
		{
			val = bounds2.center;
			val.y = bounds2.min.y + ShelfProductSwapperPlugin.RackEmptyMarkerYOffset.Value;
			num = Mathf.Max(size, bounds2.extents.x + 0.04f);
			num2 = Mathf.Max(size, bounds2.extents.z + 0.04f);
		}
		else
		{
			val = CandidatePosition(slot);
			val.y += ShelfProductSwapperPlugin.RackEmptyMarkerYOffset.Value;
		}
		marker.startWidth = width;
		marker.endWidth = width;
		marker.startColor = color;
		marker.endColor = color;
		marker.SetPosition(0, val + new Vector3(0f - num, 0f, 0f - num2));
		marker.SetPosition(1, val + new Vector3(num, 0f, 0f - num2));
		marker.SetPosition(2, val + new Vector3(num, 0f, num2));
		marker.SetPosition(3, val + new Vector3(0f - num, 0f, num2));
		((Component)marker).gameObject.SetActive(true);
	}

	private static bool TryGetRackEmptyMarkerBounds(RackSlot slot, out Bounds bounds)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Label val = (((Object)(object)slot != (Object)null) ? slot.Label : null);
		if ((Object)(object)val != (Object)null && (Object)(object)val.m_Renderer != (Object)null && ((Renderer)val.m_Renderer).enabled)
		{
			bounds = ((Renderer)val.m_Renderer).bounds;
			return true;
		}
		if ((Object)(object)slot != (Object)null)
		{
			bounds = new Bounds(slot.InteractionPosition, new Vector3(0.72f, 0.08f, 0.72f));
			return true;
		}
		bounds = default(Bounds);
		return false;
	}

	private static string Describe(SlotSnapshot snapshot)
	{
		if (!snapshot.HasProduct)
		{
			return "empty slot";
		}
		return "product " + snapshot.ProductId + " x" + snapshot.Count;
	}

	private static string Describe(RackSlotSnapshot snapshot)
	{
		if (snapshot.HasBox)
		{
			return "box " + snapshot.BoxId + " x" + snapshot.BoxCount;
		}
		if (!snapshot.HasLabel)
		{
			return "empty box rack slot";
		}
		return "empty labeled box rack slot";
	}

	private static bool CanSwap(DisplaySlot firstSlot, SlotSnapshot first, DisplaySlot secondSlot, SlotSnapshot second)
	{
		if (ShelfProductSwapperPlugin.RequireCompatibleDisplayType == null || !ShelfProductSwapperPlugin.RequireCompatibleDisplayType.Value)
		{
			return true;
		}
		if (ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes != null && ShelfProductSwapperPlugin.AllowIncompatibleDisplayTypes.Value)
		{
			return true;
		}
		if (CanMoveTo(first, secondSlot))
		{
			return CanMoveTo(second, firstSlot);
		}
		return false;
	}

	private static bool CanMoveTo(SlotSnapshot source, DisplaySlot targetSlot)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (!source.HasProduct)
		{
			return true;
		}
		Display val = (((Object)(object)targetSlot != (Object)null) ? targetSlot.Display : null);
		if ((Object)(object)val == (Object)null)
		{
			return false;
		}
		return source.ProductDisplayType == val.DisplayType;
	}
}