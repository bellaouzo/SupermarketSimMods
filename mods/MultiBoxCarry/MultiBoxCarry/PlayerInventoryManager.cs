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

		Inventories.Remove(((Object)player).GetInstanceID());
	}

	public static void Reset()
	{
		Inventories.Clear();
	}
}
