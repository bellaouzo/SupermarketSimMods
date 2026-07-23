namespace MultiBoxCarry;

internal static class PlayerInventoryManager
{
	public static BoxInventory Inventory { get; private set; } = new BoxInventory();

	public static void Reset()
	{
		Inventory = new BoxInventory();
	}

	public static void ResetAndRelease()
	{
		BoxInventory inventory = Inventory;
		if (inventory != null)
		{
			for (int i = 0; i < inventory.QueuedBoxes.Count; i++)
			{
				NetworkBoxSync.MarkReleased(inventory.QueuedBoxes[i]);
			}
		}

		Reset();
	}
}
