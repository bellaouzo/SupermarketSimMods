using System;

using HarmonyLib;

using Il2CppInterop.Runtime.InteropTypes;

using UnityEngine;



namespace MultiBoxCarry;



[HarmonyPatch(typeof(Box), nameof(Box.UpdateMatchingDisplaySlotsHighlight))]

internal static class BoxHighlightPatch

{

	private static bool Prefix(Box __instance)

	{

		if ((Object)(object)__instance == (Object)null)

		{

			return true;

		}



		if (HandHighlightGuard.HighlightBypassDepth > 0)

		{

			return true;

		}



		return HandHighlightGuard.IsAuthoritativeHandBox(__instance);

	}

}



[HarmonyPatch(typeof(Box), nameof(Box.DropBox))]

internal static class BoxDropHighlightPatch

{

	private static void Prefix(Box __instance)

	{

		if ((Object)(object)__instance == (Object)null)

		{

			return;

		}



		BoxUtility.EnableWorldCollisions(__instance);

	}



	private static void Postfix(Box __instance)

	{

		if ((Object)(object)__instance == (Object)null)

		{

			return;

		}



		BoxUtility.ClearMatchingHighlight(__instance);

		PlayerObjectHolder holder = CoopPlayer.GetLocalHolder();

		if ((Object)(object)holder != (Object)null && BoxUtility.IsOnHoldPoint(__instance, holder))

		{

			return;

		}



		BoxAdapter adapter = new BoxAdapter(__instance);

		Transform parent = ((Component)__instance).transform.parent;

		if ((Object)(object)parent != (Object)null

			&& ((Object)(object)((Component)parent).GetComponent<RackSlot>() != (Object)null

				|| (Object)(object)((Component)parent).GetComponentInParent<Rack>() != (Object)null))

		{

			NetworkBoxUtil.ClearOccupyFlags(adapter);

			return;

		}



		NetworkBoxUtil.MarkReleased(adapter);

	}

}



internal static class HandHighlightGuard

{

	internal static int HighlightBypassDepth;



	private static int _lastHandBoxId = int.MinValue;

	private static int _lastHandProductId = int.MinValue;

	private static float _nextForcedHighlightAt;

	private static float _nextEnforceAt;

	private static PlayerInteraction _cachedPlayer;

	private static PlayerObjectHolder _cachedHolder;

	private static BoxInteraction _cachedBoxInteraction;



	internal static void Tick(PlayerInteraction player)

	{

		if ((Object)(object)player == (Object)null)

		{

			return;

		}



		float now = Time.unscaledTime;

		if (now < _nextEnforceAt)

		{

			return;

		}



		_nextEnforceAt = now + 0.25f;

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);

		if (inventory == null || inventory.IsEmpty)

		{

			_lastHandBoxId = int.MinValue;

			_lastHandProductId = int.MinValue;

			return;

		}



		try

		{

			Enforce(player, inventory);

		}

		catch (Exception ex)

		{

			Plugin.Log.LogWarning((object)("[HandHighlightGuard] " + ex.Message));

		}

	}



	internal static bool IsAuthoritativeHandBox(Box box)

	{

		if ((Object)(object)box == (Object)null)

		{

			return false;

		}



		PlayerInteraction player = CoopPlayer.CachedLocal;

		if ((Object)(object)player == (Object)null)

		{

			player = CoopPlayer.GetLocalPlayerInteraction();

		}



		if ((Object)(object)player == (Object)null)

		{

			return true;

		}



		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);

		if (inventory == null || inventory.IsEmpty)

		{

			return true;

		}



		CacheComponents(player);

		Box handBox = GetHandBox(_cachedHolder);

		if ((Object)(object)handBox != (Object)null)

		{

			return (Object)(object)handBox == (Object)(object)box;

		}



		return !IsQueuedBox(inventory, box);

	}



	private static void Enforce(PlayerInteraction player, BoxInventory inventory)

	{

		CacheComponents(player);

		Box handBox = GetHandBox(_cachedHolder);



		if ((Object)(object)handBox == (Object)null)

		{

			_lastHandBoxId = int.MinValue;

			_lastHandProductId = int.MinValue;

			return;

		}



		if ((Object)(object)_cachedBoxInteraction != (Object)null

			&& (Object)(object)_cachedBoxInteraction.m_Box != (Object)(object)handBox)

		{

			_cachedBoxInteraction.m_Box = handBox;

		}



		int handBoxId = ((Object)handBox).GetInstanceID();

		int productId = GetProductId(handBox);

		bool handChanged = handBoxId != _lastHandBoxId || productId != _lastHandProductId;

		_lastHandBoxId = handBoxId;

		_lastHandProductId = productId;



		float now = Time.unscaledTime;

		bool modeWrong = handBox.m_ActiveHighlightMode != Box.HighlightMode.Display;

		if (handChanged || (modeWrong && now >= _nextForcedHighlightAt))

		{

			_nextForcedHighlightAt = now + 0.5f;

			RefreshHandHighlight(handBox);

		}

	}



	private static void CacheComponents(PlayerInteraction player)

	{

		if ((Object)(object)_cachedPlayer == (Object)(object)player

			&& (Object)(object)_cachedHolder != (Object)null)

		{

			return;

		}



		_cachedPlayer = player;

		_cachedHolder = ((Component)player).GetComponent<PlayerObjectHolder>();

		_cachedBoxInteraction = ((Component)player).GetComponent<BoxInteraction>();

	}



	private static void RefreshHandHighlight(Box handBox)

	{

		handBox.m_ActiveHighlightMode = Box.HighlightMode.None;

		handBox.UpdateMatchingDisplaySlotsHighlight();

	}



	private static int GetProductId(Box box)

	{

		try

		{

			ProductSO product = box.Product;

			if ((Object)(object)product == (Object)null)

			{

				return int.MinValue;

			}



			return product.ID;

		}

		catch

		{

			return int.MinValue;

		}

	}



	private static bool IsQueuedBox(BoxInventory inventory, Box box)

	{

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



	private static Box GetHandBox(PlayerObjectHolder holder)

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



		return current.GetComponent<Box>();

	}

}


