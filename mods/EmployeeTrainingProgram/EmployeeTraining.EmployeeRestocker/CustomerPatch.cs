using HarmonyLib;

namespace EmployeeTraining.EmployeeRestocker;

[HarmonyPatch(typeof(Customer))]
public static class CustomerPatch
{
	[HarmonyPatch("StartShopping")]
	[HarmonyPostfix]
	public static void StartShopping_Postfix(Customer __instance)
	{
		ShoppingCustomerList.Instance?.StartShopping(__instance);
	}

	[HarmonyPatch("FinishShopping")]
	[HarmonyPostfix]
	public static void FinishShopping_Postfix(Customer __instance)
	{
		ShoppingCustomerList.Instance?.FinishShopping(__instance);
	}

	[HarmonyPatch("OnDisable")]
	[HarmonyPostfix]
	public static void OnDisable_Postfix(Customer __instance)
	{
		ShoppingCustomerList.Instance?.FinishShopping(__instance);
	}
}
