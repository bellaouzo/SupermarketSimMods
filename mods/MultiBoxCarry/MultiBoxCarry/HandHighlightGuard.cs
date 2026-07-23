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
		BoxAdapter adapter = new BoxAdapter(__instance);
		NetworkBoxUtil.MarkReleased(adapter);
	}
}

internal static class HandHighlightGuard
{
	internal static int HighlightBypassDepth;

	private static int _lastHandBoxId = int.MinValue;
	private static int _lastHandProductId = int.MinValue;

	internal static void Tick(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null || !CoopPlayer.IsLocal(player))
		{
			return;
		}

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

		PlayerInteraction player = CoopPlayer.GetLocalPlayerInteraction();
		if ((Object)(object)player == (Object)null)
		{
			return true;
		}

		BoxInventory inventory = PlayerInventoryManager.GetInventory(player);
		if (inventory == null || inventory.IsEmpty)
		{
			return true;
		}

		Box handBox = GetHandBox(((Component)player).GetComponent<PlayerObjectHolder>());
		if ((Object)(object)handBox != (Object)null)
		{
			return (Object)(object)handBox == (Object)(object)box;
		}

		return !IsQueuedBox(inventory, box);
	}

	private static void Enforce(PlayerInteraction player, BoxInventory inventory)
	{
		PlayerObjectHolder holder = ((Component)player).GetComponent<PlayerObjectHolder>();
		BoxInteraction boxInteraction = ((Component)player).GetComponent<BoxInteraction>();
		Box handBox = GetHandBox(holder);

		if ((Object)(object)handBox == (Object)null)
		{
			_lastHandBoxId = int.MinValue;
			_lastHandProductId = int.MinValue;
			return;
		}

		if ((Object)(object)boxInteraction != (Object)null
			&& (Object)(object)boxInteraction.m_Box != (Object)(object)handBox)
		{
			boxInteraction.m_Box = handBox;
		}

		int handBoxId = ((Object)handBox).GetInstanceID();
		int productId = GetProductId(handBox);
		bool handChanged = handBoxId != _lastHandBoxId || productId != _lastHandProductId;
		_lastHandBoxId = handBoxId;
		_lastHandProductId = productId;

		if (handChanged || handBox.m_ActiveHighlightMode != Box.HighlightMode.Display)
		{
			RefreshHandHighlight(handBox);
		}
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
