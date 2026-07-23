using System;
using System.Globalization;
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

	private static bool _bridgeCreated;
	private static bool _eventHooked;
	private static Il2CppSystem.Action<EventData> _eventHandler;

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
				return;
			}

			Raise(DisplaySwapEventCode, payload);
			PushDisplayAuthoritySync(first);
			PushDisplayAuthoritySync(second);
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)("Display network sync failed: " + ex.Message));
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
			if (TryBuildRackPayload(first, second, out string payload))
			{
				Raise(RackSwapEventCode, payload);
			}

			PushRackAuthoritySync(first);
			PushRackAuthoritySync(second);
		}
		catch (Exception ex)
		{
			ShelfProductSwapperPlugin.LogSource.LogWarning((object)("Rack network sync failed: " + ex.Message));
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
		NetworkDisplay network = GetNetworkDisplay(slot);
		PhotonView view = network != null ? network.PhotonView : null;
		if ((Object)(object)view != (Object)null && !view.IsMine)
		{
			view.RequestOwnership();
		}
	}

	private static void RequestOwnership(RackSlot slot)
	{
		NetworkRack network = GetNetworkRack(slot);
		PhotonView view = network != null ? network.PhotonView : null;
		if ((Object)(object)view != (Object)null && !view.IsMine)
		{
			view.RequestOwnership();
		}
	}

	private static void PushDisplayAuthoritySync(DisplaySlot slot)
	{
		NetworkDisplay network = GetNetworkDisplay(slot);
		if ((Object)(object)network == (Object)null)
		{
			return;
		}

		Player[] players = PhotonNetwork.PlayerList;
		if (players == null)
		{
			return;
		}

		foreach (Player player in players)
		{
			if (player == null || player.IsLocal || string.IsNullOrEmpty(player.UserId))
			{
				continue;
			}

			network.RequestDisplayData_Broadcast(player.UserId);
		}
	}

	private static void PushRackAuthoritySync(RackSlot slot)
	{
		NetworkRack network = GetNetworkRack(slot);
		if ((Object)(object)network == (Object)null)
		{
			return;
		}

		network.RackDataUpdate_Broadcast();
		network.SyncAllBoxes_Broadcast();
	}

	private static bool TryBuildDisplayPayload(DisplaySlot first, DisplaySlot second, out string payload)
	{
		payload = null;
		if (!TryDescribeDisplaySlot(first, out int view1, out int index1, out int product1, out int count1, out int label1, out float price1)
			|| !TryDescribeDisplaySlot(second, out int view2, out int index2, out int product2, out int count2, out int label2, out float price2))
		{
			return false;
		}

		payload = string.Join("|",
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
		if (!TryDescribeRackSlot(first, out int view1, out int index1, out int boxId1, out int product1, out int count1, out int label1)
			|| !TryDescribeRackSlot(second, out int view2, out int index2, out int boxId2, out int product2, out int count2, out int label2))
		{
			return false;
		}

		payload = string.Join("|",
			view1.ToString(CultureInfo.InvariantCulture),
			index1.ToString(CultureInfo.InvariantCulture),
			boxId1.ToString(CultureInfo.InvariantCulture),
			product1.ToString(CultureInfo.InvariantCulture),
			count1.ToString(CultureInfo.InvariantCulture),
			label1.ToString(CultureInfo.InvariantCulture),
			view2.ToString(CultureInfo.InvariantCulture),
			index2.ToString(CultureInfo.InvariantCulture),
			boxId2.ToString(CultureInfo.InvariantCulture),
			product2.ToString(CultureInfo.InvariantCulture),
			count2.ToString(CultureInfo.InvariantCulture),
			label2.ToString(CultureInfo.InvariantCulture));
		return true;
	}

	private static void ApplyDisplayPayload(string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			return;
		}

		string[] parts = payload.Split('|');
		if (parts.Length < 12)
		{
			return;
		}

		ApplyDisplaySlotState(
			ParseInt(parts[0]), ParseInt(parts[1]), ParseInt(parts[2]), ParseInt(parts[3]), ParseInt(parts[4]), ParseFloat(parts[5]));
		ApplyDisplaySlotState(
			ParseInt(parts[6]), ParseInt(parts[7]), ParseInt(parts[8]), ParseInt(parts[9]), ParseInt(parts[10]), ParseFloat(parts[11]));
		ShelfProductSwapperPlugin.LogSource.LogInfo((object)"Applied remote shelf swap.");
	}

	private static void ApplyRackPayload(string payload)
	{
		if (string.IsNullOrEmpty(payload))
		{
			return;
		}

		string[] parts = payload.Split('|');
		if (parts.Length < 12)
		{
			return;
		}

		ApplyRackSlotState(
			ParseInt(parts[0]), ParseInt(parts[1]), ParseInt(parts[2]), ParseInt(parts[3]), ParseInt(parts[4]), ParseInt(parts[5]));
		ApplyRackSlotState(
			ParseInt(parts[6]), ParseInt(parts[7]), ParseInt(parts[8]), ParseInt(parts[9]), ParseInt(parts[10]), ParseInt(parts[11]));
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

	private static void ApplyRackSlotState(int viewId, int slotIndex, int boxId, int productId, int boxCount, int labelProductId)
	{
		if (!TryResolveRackSlot(viewId, slotIndex, out RackSlot slot))
		{
			return;
		}

		ShelfProductSwapperRuntime.ApplyRemoteRackSlot(slot, boxId, productId, boxCount, labelProductId);
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

	private static bool TryDescribeRackSlot(RackSlot slot, out int viewId, out int slotIndex, out int boxId, out int productId, out int boxCount, out int labelProductId)
	{
		viewId = 0;
		slotIndex = -1;
		boxId = -1;
		productId = -1;
		boxCount = 0;
		labelProductId = -1;
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

		boxCount = slot.Boxes != null ? slot.Boxes.Count : 0;
		boxId = slot.CurrentBoxID;
		productId = slot.Data != null ? slot.Data.ProductID : -1;
		if (productId < 0 && boxCount > 0)
		{
			Box peak = slot.PeakBoxFromRack();
			if ((Object)(object)peak != (Object)null && peak.Data != null)
			{
				productId = peak.Data.ProductID;
			}
		}

		labelProductId = slot.Data != null ? slot.Data.ProductID : -1;
		return true;
	}

	private static bool TryResolveDisplaySlot(int viewId, int slotIndex, out DisplaySlot slot)
	{
		slot = null;
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

	private static int ParseInt(string value)
	{
		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : 0;
	}

	private static float ParseFloat(string value)
	{
		return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
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
		NetworkShelfSync.TryHookPhotonEvents();
	}
}
