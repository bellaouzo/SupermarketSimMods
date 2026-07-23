using HarmonyLib;
using MyBox;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch(typeof(DisplaySlot))]
public static class DisplaySlotPatch
{
	[HarmonyPatch("RemoveFromDisplayManagerWhileCarrying")]
	[HarmonyPostfix]
	public static void RemoveFromDisplayManagerWhileCarrying_Postfix(DisplaySlot __instance)
	{
		ItemQuantity data = __instance.m_ProductCountData ?? __instance.Data;
		if (data != null && data.Products != null && data.Products.Count > 0)
		{
			Singleton<InventoryManager>.Instance.RemoveProductFromDisplay(data);
		}
	}

	[HarmonyPatch("AddBackToDisplayManagerAfterPlaced")]
	[HarmonyPostfix]
	public static void AddBackToDisplayManagerAfterPlaced_Postfix(DisplaySlot __instance)
	{
		ItemQuantity data = __instance.m_ProductCountData ?? __instance.Data;
		if (data != null && data.Products != null && data.Products.Count > 0)
		{
			Singleton<InventoryManager>.Instance.AddProductToDisplay(data);
		}
	}
}
