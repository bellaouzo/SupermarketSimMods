using System;
using System.Globalization;
using System.Text;
using __Project__.Scripts.Multiplayer;
using ExitGames.Client.Photon;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SupermarketSimulatorShelfProductSwapper;

internal static class NetworkShelfSync
{
	internal const byte DisplaySwapEventCode = 191;
	internal const byte RackSwapEventCode = 192;

	private const string HandshakeKey = "sps_hs";
	private const float OwnershipTimeoutSeconds = 0.5f;
	private const float ViewLockSeconds = 0.3f;

	private static bool _bridgeCreated;
	private static bool _eventHooked;
	private static Il2CppSystem.Action<EventData> _eventHandler;

	private static bool _wasInRoom;
	private static bool _peersMatch = true;
	private static string _lastHandshakeWarn = string.Empty;

	private static int _nextSeq = 1;
	private static readonly System.Collections.Generic.Dictionary<string, int> LastAppliedSeq =
		new System.Collections.Generic.Dictionary<string, int>();
	private static readonly System.Collections.Generic.Dictionary<int, float> ViewLocks =
		new System.Collections.Generic.Dictionary<int, float>();

	private static bool _pendingActive;
	private static bool _pendingIsRack;
	private static DisplaySlot _pendingDisplayA;
	private static DisplaySlot _pendingDisplayB;
	private static RackSlot _pendingRackA;
	private static RackSlot _pendingRackB;
	private static float _pendingDeadline;
	private static int _pendingViewA;
	private static int _pendingViewB;

	internal static bool InMultiplayer
	{
		get
		{
			try
			{
				return PhotonNetwork.InRoom;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static bool IsHost
	{
		get
		{
			if (!InMultiplayer)
			{
				return true;
			}

			try
			{
				return PhotonNetwork.IsMasterClient;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static bool PeersMatch => !InMultiplayer || _peersMatch;

	internal static bool CanSwapInMultiplayer => !InMultiplayer || _peersMatch;

	internal static void EnsureCallbacks()
	{
		if (_bridgeCreated)
		{
			return;
		}

		try
		{
			ClassInjector.RegisterTypeInIl2Cpp<SwapEventBridge>();
			GameObject go = new GameObject("ShelfSwapper_NetworkBridge");
			Object.DontDestroyOnLoad(go);
			go.AddComponent<SwapEventBridge>();
			_bridgeCreated = true;
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)("Could not register swap network bridge: " + ex.Message));
		}
	}

	internal static void Tick()
	{
		TickHandshake();
		TickPendingOwnership();
		TryHookPhotonEvents();
	}

	internal static void TryHookPhotonEvents()
	{
		if (_eventHooked)
		{
			return;
		}

		try
		{
			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			if (client == null)
			{
				return;
			}

			_eventHandler = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<EventData>>(
				(Action<EventData>)HandleEvent);
			client.add_EventReceived(_eventHandler);
			_eventHooked = true;
			ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap Photon event hook ready.");
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)("Photon event hook failed: " + ex.Message));
		}
	}

	internal static bool TryBeginDisplaySwap(DisplaySlot first, DisplaySlot second)
	{
		if (!InMultiplayer)
		{
			return true;
		}

		if (!CanSwapInMultiplayer)
		{
			WarnPeersMismatch();
			return false;
		}

		if (!TryGetViewId(first, out int viewA) || !TryGetViewId(second, out int viewB))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Shelf swap blocked: missing PhotonView on display.");
			return false;
		}

		if (!TryLockViews(viewA, viewB))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Shelf swap blocked: display already being swapped.");
			return false;
		}

		RequestOwnership(first);
		RequestOwnership(second);
		if (IsMine(first) && IsMine(second))
		{
			return true;
		}

		_pendingActive = true;
		_pendingIsRack = false;
		_pendingDisplayA = first;
		_pendingDisplayB = second;
		_pendingRackA = null;
		_pendingRackB = null;
		_pendingViewA = viewA;
		_pendingViewB = viewB;
		_pendingDeadline = Time.realtimeSinceStartup + OwnershipTimeoutSeconds;
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Shelf swap waiting for Photon ownership...");
		return false;
	}

	internal static bool TryBeginRackSwap(RackSlot first, RackSlot second)
	{
		if (!InMultiplayer)
		{
			return true;
		}

		if (!CanSwapInMultiplayer)
		{
			WarnPeersMismatch();
			return false;
		}

		if (!TryGetViewId(first, out int viewA) || !TryGetViewId(second, out int viewB))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Box rack swap blocked: missing PhotonView on rack.");
			return false;
		}

		if (!TryLockViews(viewA, viewB))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Box rack swap blocked: rack already being swapped.");
			return false;
		}

		RequestOwnership(first);
		RequestOwnership(second);
		if (IsMine(first) && IsMine(second))
		{
			return true;
		}

		_pendingActive = true;
		_pendingIsRack = true;
		_pendingRackA = first;
		_pendingRackB = second;
		_pendingDisplayA = null;
		_pendingDisplayB = null;
		_pendingViewA = viewA;
		_pendingViewB = viewB;
		_pendingDeadline = Time.realtimeSinceStartup + OwnershipTimeoutSeconds;
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Box rack swap waiting for Photon ownership...");
		return false;
	}

	internal static void SyncDisplayAfterSwap(DisplaySlot first, DisplaySlot second)
	{
		if (!InMultiplayer || (Object)(object)first == (Object)null || (Object)(object)second == (Object)null)
		{
			return;
		}

		try
		{
			RequestOwnership(first);
			RequestOwnership(second);
			if (!TryBuildDisplayPayload(first, second, out string payload))
			{
				ShelfProductSwapperPlugin.LogSource.LogError(
					(object)"Shelf swap network sync FAILED after local mutate: could not build display payload. Co-op state may have diverged.");
				return;
			}

			Raise(DisplaySwapEventCode, payload);
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogError((object)("Display network sync failed after local mutate: " + ex.Message));
		}
	}

	internal static void SyncRackAfterSwap(RackSlot first, RackSlot second)
	{
		if (!InMultiplayer || (Object)(object)first == (Object)null || (Object)(object)second == (Object)null)
		{
			return;
		}

		try
		{
			RequestOwnership(first);
			RequestOwnership(second);
			if (!TryBuildRackPayload(first, second, out string payload))
			{
				ShelfProductSwapperPlugin.LogSource.LogError(
					(object)"Box rack swap network sync FAILED after local mutate: could not build rack payload. Co-op state may have diverged.");
				return;
			}

			Raise(RackSwapEventCode, payload);
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogError((object)("Rack network sync failed after local mutate: " + ex.Message));
		}
	}

	internal static void HandleEvent(EventData photonEvent)
	{
		if (photonEvent == null)
		{
			return;
		}

		try
		{
			string payload = photonEvent.CustomData != null ? photonEvent.CustomData.ToString() : null;
			if (photonEvent.Code == DisplaySwapEventCode)
			{
				ApplyDisplayPayload(payload);
			}
			else if (photonEvent.Code == RackSwapEventCode)
			{
				ApplyRackPayload(payload);
			}
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)("Swap event apply failed: " + ex.Message));
		}
	}

	private static void TickHandshake()
	{
		bool inRoom = InMultiplayer;
		if (inRoom && !_wasInRoom)
		{
			if (IsHost)
			{
				PublishHandshake();
			}
			else
			{
				UpdatePeersMatch();
			}
		}
		else if (!inRoom && _wasInRoom)
		{
			_peersMatch = true;
			_lastHandshakeWarn = string.Empty;
			ClearPendingOwnership(abort: false);
			LastAppliedSeq.Clear();
			ViewLocks.Clear();
		}

		_wasInRoom = inRoom;
		if (!inRoom)
		{
			return;
		}

		UpdatePeersMatch();
		if (IsHost)
		{
			PublishHandshake();
		}
	}

	private static void TickPendingOwnership()
	{
		if (!_pendingActive)
		{
			return;
		}

		if (_pendingIsRack)
		{
			if ((Object)(object)_pendingRackA == (Object)null || (Object)(object)_pendingRackB == (Object)null)
			{
				ClearPendingOwnership(abort: true);
				return;
			}

			if (IsMine(_pendingRackA) && IsMine(_pendingRackB))
			{
				RackSlot a = _pendingRackA;
				RackSlot b = _pendingRackB;
				ClearPendingOwnership(abort: false);
				ShelfProductSwapperRuntime.CompletePendingRackSwap(a, b);
				return;
			}
		}
		else
		{
			if ((Object)(object)_pendingDisplayA == (Object)null || (Object)(object)_pendingDisplayB == (Object)null)
			{
				ClearPendingOwnership(abort: true);
				return;
			}

			if (IsMine(_pendingDisplayA) && IsMine(_pendingDisplayB))
			{
				DisplaySlot a = _pendingDisplayA;
				DisplaySlot b = _pendingDisplayB;
				ClearPendingOwnership(abort: false);
				ShelfProductSwapperRuntime.CompletePendingDisplaySwap(a, b);
				return;
			}
		}

		if (Time.realtimeSinceStartup >= _pendingDeadline)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning(
				(object)"Shelf swap aborted: Photon ownership timeout (0.5s). Try again.");
			ClearPendingOwnership(abort: true);
		}
	}

	private static void ClearPendingOwnership(bool abort)
	{
		if (abort && _pendingActive)
		{
			ReleaseViewLocks(_pendingViewA, _pendingViewB);
		}

		_pendingActive = false;
		_pendingDisplayA = null;
		_pendingDisplayB = null;
		_pendingRackA = null;
		_pendingRackB = null;
		_pendingViewA = 0;
		_pendingViewB = 0;
	}

	private static void PublishHandshake()
	{
		if (!IsHost)
		{
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room == null)
			{
				return;
			}

			string value = ShelfProductSwapperPlugin.PluginVersion;
			object existing = null;
			if (room.CustomProperties != null && room.CustomProperties.ContainsKey(HandshakeKey))
			{
				existing = room.CustomProperties[HandshakeKey];
			}

			if (existing != null && existing.ToString() == value)
			{
				return;
			}

			Hashtable props = new Hashtable { [HandshakeKey] = value };
			room.SetCustomProperties(props);
		}
		catch
		{
		}
	}

	private static void UpdatePeersMatch()
	{
		if (IsHost)
		{
			_peersMatch = true;
			return;
		}

		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			int playerCount = room != null ? room.PlayerCount : 0;
			bool hasOthers = playerCount > 1;
			if (room?.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakeKey))
			{
				_peersMatch = !hasOthers;
				if (!_peersMatch && _lastHandshakeWarn != "missing")
				{
					_lastHandshakeWarn = "missing";
					ShelfProductSwapperPlugin.LogSource.LogWarning(
						(object)("ShelfProductSwapper handshake missing (expected sps_hs="
							+ ShelfProductSwapperPlugin.PluginVersion
							+ "). Install matching CS-ShelfProductSwapper.dll on all PCs. Swap blocked."));
				}

				return;
			}

			string expected = ShelfProductSwapperPlugin.PluginVersion;
			string actual = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			_peersMatch = actual == expected;
			if (!_peersMatch && actual != _lastHandshakeWarn)
			{
				_lastHandshakeWarn = actual;
				ShelfProductSwapperPlugin.LogSource.LogWarning(
					(object)("ShelfProductSwapper version mismatch with host. Local=" + expected
						+ " Host=" + actual + ". Install the same CS-ShelfProductSwapper.dll on all PCs. Swap blocked."));
			}
			else if (_peersMatch)
			{
				_lastHandshakeWarn = string.Empty;
			}
		}
		catch
		{
			_peersMatch = true;
		}
	}

	private static void WarnPeersMismatch()
	{
		ShelfProductSwapperPlugin.LogSource.LogWarning(
			(object)("Shelf swap blocked: co-op handshake mismatch or missing (sps_hs="
				+ ShelfProductSwapperPlugin.PluginVersion + "). Match mods on all PCs."));
	}

	private static void Raise(byte code, string payload)
	{
		RaiseEventOptions options = new RaiseEventOptions
		{
			Receivers = ReceiverGroup.Others
		};
		PhotonNetwork.RaiseEvent(code, payload, options, SendOptions.SendReliable);
	}

	private static void RequestOwnership(DisplaySlot slot)
	{
		PhotonView view = GetPhotonView(slot);
		if ((Object)(object)view != (Object)null && !view.IsMine)
		{
			view.RequestOwnership();
		}
	}

	private static void RequestOwnership(RackSlot slot)
	{
		PhotonView view = GetPhotonView(slot);
		if ((Object)(object)view != (Object)null && !view.IsMine)
		{
			view.RequestOwnership();
		}
	}

	private static bool IsMine(DisplaySlot slot)
	{
		PhotonView view = GetPhotonView(slot);
		return (Object)(object)view == (Object)null || view.IsMine;
	}

	private static bool IsMine(RackSlot slot)
	{
		PhotonView view = GetPhotonView(slot);
		return (Object)(object)view == (Object)null || view.IsMine;
	}

	private static PhotonView GetPhotonView(DisplaySlot slot)
	{
		NetworkDisplay network = GetNetworkDisplay(slot);
		return network != null ? network.PhotonView : null;
	}

	private static PhotonView GetPhotonView(RackSlot slot)
	{
		NetworkRack network = GetNetworkRack(slot);
		return network != null ? network.PhotonView : null;
	}

	private static bool TryGetViewId(DisplaySlot slot, out int viewId)
	{
		viewId = 0;
		NetworkDisplay network = GetNetworkDisplay(slot);
		if ((Object)(object)network == (Object)null)
		{
			return false;
		}

		viewId = network.ViewId;
		return viewId != 0;
	}

	private static bool TryGetViewId(RackSlot slot, out int viewId)
	{
		viewId = 0;
		NetworkRack network = GetNetworkRack(slot);
		if ((Object)(object)network == (Object)null)
		{
			return false;
		}

		viewId = network.ViewId;
		return viewId != 0;
	}

	private static bool TryLockViews(int viewA, int viewB)
	{
		float now = Time.realtimeSinceStartup;
		PruneViewLocks(now);
		if (IsViewLocked(viewA, now) || IsViewLocked(viewB, now))
		{
			return false;
		}

		float until = now + ViewLockSeconds;
		ViewLocks[viewA] = until;
		ViewLocks[viewB] = until;
		return true;
	}

	private static void ReleaseViewLocks(int viewA, int viewB)
	{
		ViewLocks.Remove(viewA);
		ViewLocks.Remove(viewB);
	}

	private static bool IsViewLocked(int viewId, float now)
	{
		return ViewLocks.TryGetValue(viewId, out float until) && until > now;
	}

	private static void PruneViewLocks(float now)
	{
		if (ViewLocks.Count == 0)
		{
			return;
		}

		System.Collections.Generic.List<int> expired = null;
		foreach (System.Collections.Generic.KeyValuePair<int, float> pair in ViewLocks)
		{
			if (pair.Value <= now)
			{
				expired ??= new System.Collections.Generic.List<int>();
				expired.Add(pair.Key);
			}
		}

		if (expired == null)
		{
			return;
		}

		for (int i = 0; i < expired.Count; i++)
		{
			ViewLocks.Remove(expired[i]);
		}
	}

	private static int NextSeq()
	{
		int seq = _nextSeq;
		if (_nextSeq == int.MaxValue)
		{
			_nextSeq = 1;
		}
		else
		{
			_nextSeq++;
		}

		return seq;
	}

	private static string PairKey(int viewA, int viewB)
	{
		return viewA <= viewB
			? viewA.ToString(CultureInfo.InvariantCulture) + ":" + viewB.ToString(CultureInfo.InvariantCulture)
			: viewB.ToString(CultureInfo.InvariantCulture) + ":" + viewA.ToString(CultureInfo.InvariantCulture);
	}

	private static bool ShouldApplySeq(int viewA, int viewB, int seq)
	{
		string key = PairKey(viewA, viewB);
		if (LastAppliedSeq.TryGetValue(key, out int last) && seq <= last)
		{
			return false;
		}

		LastAppliedSeq[key] = seq;
		return true;
	}

	private static bool TryBuildDisplayPayload(DisplaySlot first, DisplaySlot second, out string payload)
	{
		payload = null;
		if (!TryDescribeDisplaySlot(first, out int view1, out int index1, out int product1, out int count1, out int label1, out float price1)
			|| !TryDescribeDisplaySlot(second, out int view2, out int index2, out int product2, out int count2, out int label2, out float price2))
		{
			return false;
		}

		int seq = NextSeq();
		payload = string.Join("|",
			seq.ToString(CultureInfo.InvariantCulture),
			view1.ToString(CultureInfo.InvariantCulture),
			index1.ToString(CultureInfo.InvariantCulture),
			product1.ToString(CultureInfo.InvariantCulture),
			count1.ToString(CultureInfo.InvariantCulture),
			label1.ToString(CultureInfo.InvariantCulture),
			price1.ToString(CultureInfo.InvariantCulture),
			view2.ToString(CultureInfo.InvariantCulture),
			index2.ToString(CultureInfo.InvariantCulture),
			product2.ToString(CultureInfo.InvariantCulture),
			count2.ToString(CultureInfo.InvariantCulture),
			label2.ToString(CultureInfo.InvariantCulture),
			price2.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private static bool TryBuildRackPayload(RackSlot first, RackSlot second, out string payload)
	{
		payload = null;
		if (!TryDescribeRackSlot(first, out int view1, out int index1, out int label1, out int boxId1, out int boxCount1, out string boxes1)
			|| !TryDescribeRackSlot(second, out int view2, out int index2, out int label2, out int boxId2, out int boxCount2, out string boxes2))
		{
			return false;
		}

		int seq = NextSeq();
		payload = string.Join("|",
			seq.ToString(CultureInfo.InvariantCulture),
			view1.ToString(CultureInfo.InvariantCulture),
			index1.ToString(CultureInfo.InvariantCulture),
			label1.ToString(CultureInfo.InvariantCulture),
			boxId1.ToString(CultureInfo.InvariantCulture),
			boxCount1.ToString(CultureInfo.InvariantCulture),
			boxes1 ?? string.Empty,
			view2.ToString(CultureInfo.InvariantCulture),
			index2.ToString(CultureInfo.InvariantCulture),
			label2.ToString(CultureInfo.InvariantCulture),
			boxId2.ToString(CultureInfo.InvariantCulture),
			boxCount2.ToString(CultureInfo.InvariantCulture),
			boxes2 ?? string.Empty);
		return true;
	}

	private static void ApplyDisplayPayload(string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			return;
		}

		string[] parts = payload.Split('|');
		if (parts.Length < 13)
		{
			return;
		}

		if (!TryParseInt(parts[0], out int seq)
			|| !TryParseInt(parts[1], out int view1)
			|| !TryParseInt(parts[2], out int index1)
			|| !TryParseInt(parts[3], out int product1)
			|| !TryParseInt(parts[4], out int count1)
			|| !TryParseInt(parts[5], out int label1)
			|| !TryParseFloat(parts[6], out float price1)
			|| !TryParseInt(parts[7], out int view2)
			|| !TryParseInt(parts[8], out int index2)
			|| !TryParseInt(parts[9], out int product2)
			|| !TryParseInt(parts[10], out int count2)
			|| !TryParseInt(parts[11], out int label2)
			|| !TryParseFloat(parts[12], out float price2))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Dropped display swap event: parse failed.");
			return;
		}

		if (!ShouldApplySeq(view1, view2, seq))
		{
			return;
		}

		if (TryResolveDisplaySlot(view1, index1, out DisplaySlot slot1))
		{
			ShelfProductSwapperRuntime.PrepareRemoteDisplaySlot(slot1);
		}

		if (TryResolveDisplaySlot(view2, index2, out DisplaySlot slot2))
		{
			ShelfProductSwapperRuntime.PrepareRemoteDisplaySlot(slot2);
		}

		ApplyDisplaySlotState(view1, index1, product1, count1, label1, price1);
		ApplyDisplaySlotState(view2, index2, product2, count2, label2, price2);
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Applied remote shelf swap.");
	}

	private static void ApplyRackPayload(string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			return;
		}

		string[] parts = payload.Split('|');
		if (parts.Length < 13)
		{
			return;
		}

		if (!TryParseInt(parts[0], out int seq)
			|| !TryParseInt(parts[1], out int view1)
			|| !TryParseInt(parts[2], out int index1)
			|| !TryParseInt(parts[3], out int label1)
			|| !TryParseInt(parts[4], out int boxId1)
			|| !TryParseInt(parts[5], out int boxCount1)
			|| !TryParseInt(parts[7], out int view2)
			|| !TryParseInt(parts[8], out int index2)
			|| !TryParseInt(parts[9], out int label2)
			|| !TryParseInt(parts[10], out int boxId2)
			|| !TryParseInt(parts[11], out int boxCount2))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Dropped rack swap event: parse failed.");
			return;
		}

		string boxes1 = parts[6] ?? string.Empty;
		string boxes2 = parts[12] ?? string.Empty;
		if (!TryParseBoxEntries(boxes1, boxCount1, out System.Collections.Generic.List<ShelfProductSwapperRuntime.RemoteBoxEntry> entries1)
			|| !TryParseBoxEntries(boxes2, boxCount2, out System.Collections.Generic.List<ShelfProductSwapperRuntime.RemoteBoxEntry> entries2))
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)"Dropped rack swap event: box payload parse failed.");
			return;
		}

		if (!ShouldApplySeq(view1, view2, seq))
		{
			return;
		}

		ApplyRackSlotState(view1, index1, boxId1, label1, entries1);
		ApplyRackSlotState(view2, index2, boxId2, label2, entries2);
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Applied remote rack swap.");
	}

	private static void ApplyDisplaySlotState(int viewId, int slotIndex, int productId, int count, int labelProductId, float labelPrice)
	{
		if (!TryResolveDisplaySlot(viewId, slotIndex, out DisplaySlot slot))
		{
			return;
		}

		ShelfProductSwapperRuntime.ApplyRemoteDisplaySlot(slot, productId, count, labelProductId, labelPrice);
	}

	private static void ApplyRackSlotState(
		int viewId,
		int slotIndex,
		int boxId,
		int labelProductId,
		System.Collections.Generic.List<ShelfProductSwapperRuntime.RemoteBoxEntry> boxes)
	{
		if (!TryResolveRackSlot(viewId, slotIndex, out RackSlot slot))
		{
			return;
		}

		ShelfProductSwapperRuntime.ApplyRemoteRackSlot(slot, boxId, labelProductId, boxes);
	}

	private static bool TryDescribeDisplaySlot(DisplaySlot slot, out int viewId, out int slotIndex, out int productId, out int count, out int labelProductId, out float labelPrice)
	{
		viewId = 0;
		slotIndex = -1;
		productId = -1;
		count = 0;
		labelProductId = -1;
		labelPrice = 0f;
		if ((Object)(object)slot == (Object)null)
		{
			return false;
		}

		NetworkDisplay network = GetNetworkDisplay(slot);
		Display display = slot.Display;
		if ((Object)(object)network == (Object)null || (Object)(object)display == (Object)null)
		{
			return false;
		}

		viewId = network.ViewId;
		if (viewId == 0)
		{
			return false;
		}

		List<DisplaySlot> slots = display.DisplaySlots;
		if (slots == null)
		{
			return false;
		}

		for (int i = 0; i < slots.Count; i++)
		{
			if ((Object)(object)slots[i] == (Object)(object)slot)
			{
				slotIndex = i;
				break;
			}
		}

		if (slotIndex < 0)
		{
			return false;
		}

		count = Mathf.Max(0, slot.ProductCount);
		productId = count > 0 ? slot.ProductID : -1;
		if (slot.Data != null && slot.Data.HasLabel)
		{
			labelProductId = slot.Data.FirstItemID;
			labelPrice = slot.Price;
		}
		else if (productId >= 0)
		{
			labelProductId = productId;
			labelPrice = slot.Price;
		}

		return true;
	}

	private static bool TryDescribeRackSlot(
		RackSlot slot,
		out int viewId,
		out int slotIndex,
		out int labelProductId,
		out int boxId,
		out int boxCount,
		out string boxesPayload)
	{
		viewId = 0;
		slotIndex = -1;
		labelProductId = -1;
		boxId = -1;
		boxCount = 0;
		boxesPayload = string.Empty;
		if ((Object)(object)slot == (Object)null)
		{
			return false;
		}

		NetworkRack network = GetNetworkRack(slot);
		Rack rack = slot.OwnRack;
		if ((Object)(object)network == (Object)null || (Object)(object)rack == (Object)null)
		{
			return false;
		}

		viewId = network.ViewId;
		if (viewId == 0)
		{
			return false;
		}

		List<RackSlot> slots = rack.RackSlots;
		if (slots == null)
		{
			return false;
		}

		for (int i = 0; i < slots.Count; i++)
		{
			if ((Object)(object)slots[i] == (Object)(object)slot)
			{
				slotIndex = i;
				break;
			}
		}

		if (slotIndex < 0)
		{
			return false;
		}

		boxId = slot.CurrentBoxID;
		if (boxId < 0 && slot.Data != null)
		{
			boxId = slot.Data.BoxID;
		}

		labelProductId = slot.Data != null ? slot.Data.ProductID : -1;
		List<Box> boxes = slot.Boxes;
		boxCount = boxes != null ? boxes.Count : 0;
		if (boxCount <= 0)
		{
			boxesPayload = string.Empty;
			return true;
		}

		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < boxes.Count; i++)
		{
			Box box = boxes[i];
			int productId = -1;
			int productCount = 1;
			if ((Object)(object)box != (Object)null && box.Data != null)
			{
				productId = box.Data.ProductID;
				productCount = Mathf.Max(0, box.Data.ProductCount);
			}

			if (productId < 0)
			{
				productId = labelProductId;
			}

			if (productId < 0)
			{
				return false;
			}

			if (i > 0)
			{
				sb.Append(';');
			}

			sb.Append(productId.ToString(CultureInfo.InvariantCulture));
			sb.Append(':');
			sb.Append(productCount.ToString(CultureInfo.InvariantCulture));
		}

		boxesPayload = sb.ToString();
		if (labelProductId < 0)
		{
			labelProductId = boxes[0] != null && boxes[0].Data != null ? boxes[0].Data.ProductID : -1;
		}

		return true;
	}

	private static bool TryParseBoxEntries(
		string encoded,
		int expectedCount,
		out System.Collections.Generic.List<ShelfProductSwapperRuntime.RemoteBoxEntry> entries)
	{
		entries = new System.Collections.Generic.List<ShelfProductSwapperRuntime.RemoteBoxEntry>();
		if (expectedCount < 0)
		{
			return false;
		}

		if (expectedCount == 0)
		{
			return string.IsNullOrEmpty(encoded);
		}

		if (string.IsNullOrEmpty(encoded))
		{
			return false;
		}

		string[] parts = encoded.Split(';');
		if (parts.Length != expectedCount)
		{
			return false;
		}

		for (int i = 0; i < parts.Length; i++)
		{
			string part = parts[i];
			int colon = part.IndexOf(':');
			if (colon <= 0 || colon >= part.Length - 1)
			{
				return false;
			}

			if (!TryParseInt(part.Substring(0, colon), out int productId)
				|| !TryParseInt(part.Substring(colon + 1), out int count))
			{
				return false;
			}

			entries.Add(new ShelfProductSwapperRuntime.RemoteBoxEntry
			{
				ProductId = productId,
				ProductCount = Mathf.Max(0, count)
			});
		}

		return true;
	}

	private static bool TryResolveDisplaySlot(int viewId, int slotIndex, out DisplaySlot slot)
	{
		slot = null;
		if (viewId == 0)
		{
			return false;
		}

		PhotonView view = PhotonView.Find(viewId);
		if ((Object)(object)view == (Object)null)
		{
			return false;
		}

		NetworkDisplay network = view.GetComponent<NetworkDisplay>()
			?? ((Component)view).GetComponentInChildren<NetworkDisplay>(true);
		Display display = network != null ? network.Display : null;
		List<DisplaySlot> slots = display != null ? display.DisplaySlots : null;
		if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
		{
			return false;
		}

		slot = slots[slotIndex];
		return (Object)(object)slot != (Object)null;
	}

	private static bool TryResolveRackSlot(int viewId, int slotIndex, out RackSlot slot)
	{
		slot = null;
		if (viewId == 0)
		{
			return false;
		}

		PhotonView view = PhotonView.Find(viewId);
		if ((Object)(object)view == (Object)null)
		{
			return false;
		}

		NetworkRack network = view.GetComponent<NetworkRack>()
			?? ((Component)view).GetComponentInChildren<NetworkRack>(true);
		Rack rack = network != null ? network.Rack : null;
		List<RackSlot> slots = rack != null ? rack.RackSlots : null;
		if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
		{
			return false;
		}

		slot = slots[slotIndex];
		return (Object)(object)slot != (Object)null;
	}

	private static NetworkDisplay GetNetworkDisplay(DisplaySlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return null;
		}

		Display display = slot.Display;
		if ((Object)(object)display != (Object)null && (Object)(object)display.OwnNetworkDisplay != (Object)null)
		{
			return display.OwnNetworkDisplay;
		}

		return ((Component)slot).GetComponentInParent<NetworkDisplay>();
	}

	private static NetworkRack GetNetworkRack(RackSlot slot)
	{
		if ((Object)(object)slot == (Object)null)
		{
			return null;
		}

		return ((Component)slot).GetComponentInParent<NetworkRack>();
	}

	private static bool TryParseInt(string value, out int parsed)
	{
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
	}

	private static bool TryParseFloat(string value, out float parsed)
	{
		return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
	}
}

public sealed class SwapEventBridge : MonoBehaviour
{
	public SwapEventBridge(IntPtr ptr)
		: base(ptr)
	{
	}

	private void Update()
	{
		NetworkShelfSync.Tick();
	}
}
