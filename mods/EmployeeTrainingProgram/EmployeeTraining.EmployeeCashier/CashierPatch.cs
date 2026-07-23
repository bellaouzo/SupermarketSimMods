using System;
using HarmonyLib;
using Il2CppList = Il2CppSystem.Collections.Generic.List<Cashier>;

namespace EmployeeTraining.EmployeeCashier;

[HarmonyPatch]
public static class CashierPatch
{
	[HarmonyPatch(typeof(EmployeeManager), "SpawnCashier")]
	[HarmonyPostfix]
	public static void EmployeeManager_SpawnCashier_Postfix(EmployeeManager __instance, int cashierID)
	{
		Il2CppList active = __instance.ActiveCashiers ?? __instance.m_ActiveCashiers;
		if (active != null)
		{
			CashierSkillManager.Instance.Spawn(active, cashierID);
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "GetAvailableCashier")]
	[HarmonyPostfix]
	public static void EmployeeManager_GetAvailableCashier_Postfix(EmployeeManager __instance, Cashier __result)
	{
		if (__result == null)
		{
			return;
		}
		Il2CppList active = __instance.ActiveCashiers ?? __instance.m_ActiveCashiers;
		if (active != null)
		{
			CashierSkillManager.Instance.Spawn(active, __result.CashierID);
		}
	}

	[HarmonyPatch(typeof(EmployeeManager), "FireCashier")]
	[HarmonyPostfix]
	public static void EmployeeManager_FireCashier_Postfix(EmployeeManager __instance, int cashierID)
	{
		CashierSkillManager.Instance.Fire(cashierID);
	}

	[HarmonyPatch(typeof(CheckoutManager), "RemoveCashier")]
	[HarmonyPrefix]
	public static void CheckoutManager_RemoveCashier_Prefix(Cashier cashier)
	{
		if (cashier != null)
		{
			CashierSkillManager.Instance.Despawn(cashier);
		}
	}

	[HarmonyPatch(typeof(AutomatedCheckout), "StartCashierCheckout")]
	[HarmonyPriority(Priority.First)]
	[HarmonyPrefix]
	public static bool AutomatedCheckout_StartCashierCheckout_Prefix(AutomatedCheckout __instance)
	{
		try
		{
			CashierLogic.PerformScanning(__instance, __instance.m_ShoppingBag, __instance.m_Checkout, __instance.m_CashierSFX);
			return false;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Custom cashier scanning failed, using vanilla: {ex}");
			return true;
		}
	}

	[HarmonyPatch(typeof(AutomatedCheckout), nameof(AutomatedCheckout.FinishCheckout), new Type[] { typeof(bool) })]
	[HarmonyPriority(Priority.First)]
	[HarmonyPrefix]
	public static void AutomatedCheckout_FinishCheckout_Prefix(AutomatedCheckout __instance)
	{
		try
		{
			PaymentDuration paymentDuration = CashierLogic.FinishCheckout(__instance);
			__instance.m_IntervalAfterScanningAll = paymentDuration.IntervalAfterScanningAll;
			__instance.m_TakingPaymentInterval = paymentDuration.TakingPaymentInterval;
			__instance.m_FinishingPaymentDuration = paymentDuration.FinishingPaymentDuration;
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Cashier finish timing failed: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Checkout), nameof(Checkout.ProductScanned))]
	[HarmonyPostfix]
	public static void Checkout_ProductScanned_Postfix(Checkout __instance, bool cashier)
	{
		if (!cashier || __instance == null)
		{
			return;
		}
		try
		{
			Cashier cashierEmp = __instance.Cashier;
			if (cashierEmp != null)
			{
				CashierLogic.GiveScanExp(cashierEmp);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Cashier scan XP skipped: {ex.Message}");
		}
	}

	[HarmonyPatch(typeof(Checkout), nameof(Checkout.CashierCompletedCheckout))]
	[HarmonyPostfix]
	public static void Checkout_CashierCompletedCheckout_Postfix(Checkout __instance)
	{
		if (__instance == null)
		{
			return;
		}
		try
		{
			Cashier cashierEmp = __instance.Cashier;
			if (cashierEmp != null)
			{
				CashierLogic.GiveCheckoutBonusExp(cashierEmp);
			}
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Cashier checkout XP skipped: {ex.Message}");
		}
	}
}
