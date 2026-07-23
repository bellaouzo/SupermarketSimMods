using System.Collections.Generic;
using UnityEngine;

namespace MultiBoxCarry;

internal static class PlayerInventoryManager
{
	private static readonly Dictionary<int, BoxInventory> Inventories = new Dictionary<int, BoxInventory>();

	public static BoxInventory Inventory => GetInventory(CoopPlayer.GetLocalPlayerInteraction());

	public static BoxInventory GetInventory(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return null;
		}

		int id = ((Object)player).GetInstanceID();
		if (!Inventories.TryGetValue(id, out BoxInventory inventory) || inventory == null)
		{
			inventory = new BoxInventory();
			Inventories[id] = inventory;
		}

		return inventory;
	}

	public static void Clear(PlayerInteraction player)
	{
		if ((Object)(object)player == (Object)null)
		{
			return;
		}

		int id = ((Object)player).GetInstanceID();
		if (Inventories.TryGetValue(id, out BoxInventory inventory) && inventory != null)
		{
			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				NetworkBoxUtil.MarkReleased(inventory.QueuedBoxes[i]);
			}

			inventory.Clear();
		}

		Inventories.Remove(id);
	}

	public static void ClearAll()
	{
		FlushAll();
	}

	public static void FlushAll()
	{
		foreach (KeyValuePair<int, BoxInventory> pair in Inventories)
		{
			BoxInventory inventory = pair.Value;
			if (inventory == null)
			{
				continue;
			}

			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				NetworkBoxUtil.MarkReleased(inventory.QueuedBoxes[i]);
			}

			inventory.Clear();
		}

		Inventories.Clear();
	}

	public static void Reset()
	{
		FlushAll();
	}
}
