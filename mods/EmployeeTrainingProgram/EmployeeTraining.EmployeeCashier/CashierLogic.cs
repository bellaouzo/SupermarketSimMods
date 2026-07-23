using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using UnityEngine;
using Il2CppListFloat = Il2CppSystem.Collections.Generic.List<float>;

namespace EmployeeTraining.EmployeeCashier;

public static class CashierLogic
{
	public static void PerformScanning(AutomatedCheckout __instance, GameObject m_ShoppingBag, Checkout m_Checkout, SFXInstance m_CashierSFX)
	{
		Cashier m_Cashier = __instance.Cashier;
		CashierSkill skill = CashierSkillManager.Instance.GetOrAssignSkill(m_Cashier);
		float bootMult = skill.GetScanBoostMultiplier();
		float valueMin = skill.IntervalMin * Mathf.Lerp(1f, bootMult, 0.6f);
		float valueMax = skill.IntervalMax * bootMult;
		int seedBase = unchecked(
			((Object)__instance).GetInstanceID() * 397
			^ (m_Cashier != null ? m_Cashier.CashierID * 7919 : 0)
			^ 0x5343414E);
		System.Random rng = new System.Random(seedBase);
		float scanInterval = NextInterval(rng, valueMin, valueMax);
		m_ShoppingBag.SetActive(true);
		__instance.StartCoroutine(Scanning().WrapToIl2Cpp());
		IEnumerator Scanning()
		{
			int scanIndex = 0;
			while (m_Checkout.Belt.Products.Count > 0)
			{
				yield return new WaitForSeconds(scanInterval);
				if (m_Checkout.Belt.Products.Count <= 0)
				{
					break;
				}
				Product currentProduct = m_Checkout.Belt.Products[0];
				m_Checkout.ProductScanned(currentProduct, true);
				m_Cashier.ScanAnimation();
				m_CashierSFX.PlayScanningProductSFX();
				scanIndex++;
				rng = new System.Random(unchecked(seedBase ^ (scanIndex * 104729)));
				scanInterval = NextInterval(rng, valueMin, valueMax);
			}
		}
	}

	private static float NextInterval(System.Random rng, float min, float max)
	{
		if (max < min)
		{
			float swap = min;
			min = max;
			max = swap;
		}

		return min + (float)rng.NextDouble() * (max - min);
	}

	public static PaymentDuration FinishCheckout(AutomatedCheckout instance)
	{
		Cashier cashier = instance.Cashier;
		CashierSkill orAssignSkill = CashierSkillManager.Instance.GetOrAssignSkill(cashier);
		return new PaymentDuration
		{
			IntervalAfterScanningAll = orAssignSkill.OperationSpd / 3f,
			TakingPaymentInterval = orAssignSkill.OperationSpd / 15f * 2f,
			FinishingPaymentDuration = orAssignSkill.OperationSpd / 3f * 2f
		};
	}

	public static void GiveExpAfterFinishingCheckout(AutomatedCheckout instance)
	{
		GiveCheckoutBonusExp(instance.Cashier);
	}

	public static void GiveScanExp(Cashier cashier)
	{
		if (cashier == null)
		{
			return;
		}
		try
		{
			CashierSkillManager.Instance.GetOrAssignSkill(cashier).AddExp(1);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Cashier scan XP failed: {ex.Message}");
		}
	}

	public static void GiveCheckoutBonusExp(Cashier cashier)
	{
		if (cashier == null)
		{
			return;
		}
		try
		{
			CashierSkillManager.Instance.GetOrAssignSkill(cashier).AddExp(2);
		}
		catch (Exception ex)
		{
			Plugin.LogWarn($"Cashier checkout XP failed: {ex.Message}");
		}
	}

	public static float GetScanBoostMultiplier(this CashierSkill skill)
	{
		Cashier employee = skill.Employee;
		if (employee == null)
		{
			return 1f;
		}
		return Employee.BoostStacking.IntervalMultiplier(employee.m_CashierScanIntervals, employee.m_CurrentBoostLevel);
	}
}
