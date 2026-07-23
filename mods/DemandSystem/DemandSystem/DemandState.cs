using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppListInt = Il2CppSystem.Collections.Generic.List<int>;
using Il2CppDictIntInt = Il2CppSystem.Collections.Generic.Dictionary<int, int>;

namespace DemandSystem;

internal static class DemandState
{
	private static readonly List<int> DemandedProductIds = new List<int>();

	private static System.Random _dayRng = new System.Random(1);
	private static float _overlayUntil;
	private static int _lastGeneratedDay = int.MinValue;

	internal static IReadOnlyList<int> DemandedProducts => DemandedProductIds;

	internal static bool HasActiveDemand => DemandedProductIds.Count > 0;

	internal static bool ShouldShowOverlay
	{
		get
		{
			if (!HasActiveDemand)
			{
				return false;
			}

			if (DemandPlugin.KeepOverlayVisibleForDebug.Value)
			{
				return true;
			}

			return Time.realtimeSinceStartup <= _overlayUntil;
		}
	}

	internal static void EnsureForDay(int currentDay)
	{
		GenerateForDay(currentDay);
	}

	internal static void GenerateForDay(int currentDay)
	{
		if (!DemandPlugin.Enabled.Value || currentDay < 0 || _lastGeneratedDay == currentDay)
		{
			return;
		}

		_lastGeneratedDay = currentDay;
		DemandedProductIds.Clear();
		_dayRng = new System.Random(HashDaySeed(currentDay));

		float roll = RollDayPercent();
		if (roll > Mathf.Clamp(DemandPlugin.EventChancePercent.Value, 0f, 100f))
		{
			LogDebug($"No demand event today. day={currentDay} roll={roll:0.##}");
			return;
		}

		List<int> pool = GetActiveCustomerProducts();
		if (pool.Count == 0)
		{
			LogDebug("Demand event rolled, but no active customer products were available.");
			return;
		}

		pool.Sort();
		int count = Math.Min(RollProductCount(), pool.Count);
		for (int i = 0; i < count; i++)
		{
			int index = _dayRng.Next(pool.Count);
			DemandedProductIds.Add(pool[index]);
			pool.RemoveAt(index);
		}

		DemandedProductIds.Sort();
		_overlayUntil = Time.realtimeSinceStartup + Mathf.Max(1f, DemandPlugin.OverlayDurationSeconds.Value);
		LogDebug("Demand event active (day " + currentDay + "): " + string.Join(", ", DemandedProductIds));
	}

	internal static void ApplyToShoppingList(ItemQuantity shoppingList)
	{
		if (!DemandPlugin.Enabled.Value || !HasActiveDemand || shoppingList == null)
		{
			return;
		}

		Il2CppDictIntInt products = shoppingList.Products;
		if (products == null)
		{
			return;
		}

		System.Random rng = new System.Random(HashShoppingListSeed(products));
		int chance = Mathf.Clamp(DemandPlugin.CustomerDemandChancePercent.Value, 0, 100);
		if (rng.Next(100) >= chance)
		{
			return;
		}

		int min = Math.Max(1, DemandPlugin.ExtraItemsMin.Value);
		int max = Math.Max(min, DemandPlugin.ExtraItemsMax.Value);
		foreach (int productId in DemandedProductIds)
		{
			int extra = rng.Next(min, max + 1);
			if (products.ContainsKey(productId))
			{
				products[productId] = products[productId] + extra;
			}
			else
			{
				products.Add(productId, extra);
			}

			LogDebug($"Added {extra} demand item(s) for product {productId}.");
		}
	}

	internal static string ProductName(int productId)
	{
		try
		{
			if (NoktaSingleton<LocalizationManager>.HasInstance)
			{
				string localized = NoktaSingleton<LocalizationManager>.Instance.LocalizedProductName(productId);
				if (!string.IsNullOrWhiteSpace(localized))
				{
					return localized;
				}
			}
		}
		catch
		{
		}

		ProductSO product = ProductSo(productId);
		if ((Object)(object)product == (Object)null)
		{
			return $"Product #{productId}";
		}

		if (!string.IsNullOrWhiteSpace(product.ProductName))
		{
			return product.ProductName;
		}

		if (!string.IsNullOrWhiteSpace(product.TempProductName))
		{
			return product.TempProductName;
		}

		return $"Product #{productId}";
	}

	internal static Sprite ProductSprite(int productId)
	{
		ProductSO product = ProductSo(productId);
		return product == null ? null : product.ProductIcon;
	}

	private static ProductSO ProductSo(int productId)
	{
		try
		{
			return NoktaSingleton<IDManager>.HasInstance
				? NoktaSingleton<IDManager>.Instance.ProductSO(productId)
				: null;
		}
		catch
		{
			return null;
		}
	}

	private static List<int> GetActiveCustomerProducts()
	{
		List<int> list = new List<int>();
		if (!NoktaSingleton<ProductLicenseManager>.HasInstance)
		{
			return list;
		}

		Il2CppListInt active = NoktaSingleton<ProductLicenseManager>.Instance.ActiveProducts;
		if (active == null)
		{
			return list;
		}

		for (int i = 0; i < active.Count; i++)
		{
			int id = active[i];
			ProductSO product = ProductSo(id);
			if ((Object)(object)product != (Object)null && !product.IsHiddenForCustomer)
			{
				list.Add(id);
			}
		}

		return list;
	}

	private static int RollProductCount()
	{
		if (RollDayPercent() <= Mathf.Clamp(DemandPlugin.ThreeProductsChancePercent.Value, 0f, 100f))
		{
			return 3;
		}

		if (RollDayPercent() <= Mathf.Clamp(DemandPlugin.TwoProductsChancePercent.Value, 0f, 100f))
		{
			return 2;
		}

		return 1;
	}

	private static float RollDayPercent()
	{
		return (float)(_dayRng.NextDouble() * 100.0);
	}

	private static int HashDaySeed(int day)
	{
		unchecked
		{
			return (day * 1627) ^ 0x44534D44;
		}
	}

	private static int HashShoppingListSeed(Il2CppDictIntInt products)
	{
		unchecked
		{
			int hash = HashDaySeed(_lastGeneratedDay == int.MinValue ? 0 : _lastGeneratedDay);
			hash = (hash * 397) ^ 0x43555354;
			foreach (int demandedId in DemandedProductIds)
			{
				hash = (hash * 397) ^ demandedId;
			}

			List<int> keys = new List<int>();
			foreach (Il2CppSystem.Collections.Generic.KeyValuePair<int, int> pair in products)
			{
				keys.Add(pair.Key);
			}

			keys.Sort();
			for (int i = 0; i < keys.Count; i++)
			{
				int key = keys[i];
				hash = (hash * 397) ^ key;
				hash = (hash * 397) ^ products[key];
			}

			return hash;
		}
	}

	private static void LogDebug(string message)
	{
		if (DemandPlugin.DebugLogging.Value)
		{
			DemandPlugin.LogSource.LogInfo((object)message);
		}
	}
}
