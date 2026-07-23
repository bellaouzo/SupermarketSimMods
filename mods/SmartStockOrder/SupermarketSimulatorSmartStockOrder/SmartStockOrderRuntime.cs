using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MyBox;
using UnityEngine;

namespace SupermarketSimulatorSmartStockOrder;

internal static class SmartStockOrderRuntime
{
	private sealed class ProductPlan
	{
		internal int ProductId;

		internal ProductSO Product;

		internal int UnitsPerBox;

		internal int DisplaySlotCount;

		internal int DisplayProductCount;

		internal int DisplayMissingProducts;

		internal int RackSlotCount;

		internal int RackBoxCount;

		internal int RackProductCount;

		internal int RackMissingBoxes;

		internal int CrateCount;

		internal int CrateProductCount;

		internal int CrateMissingProducts;
	}

	private static MethodInfo _addProductMethod;

	private static TabletDevice _activeTablet;

	private static ScannerDevice _activeScanner;

	private static bool _tabletVisible;

	private static int _lastHotkeyFrame = -1;

	private static float _lastTabletActionTime;

	private static string _lastTabletActionText = "";

	private static IDManager _cachedIdManager;

	private static float _nextIdManagerLookup;

	private static DisplaySlot[] _displaySlotCache;

	private static RackSlot[] _rackSlotCache;

	private static Crate[] _crateCache;

	private static float _planSceneCacheTime = -1f;

	private const float PlanSceneCacheTtl = 1.5f;

	internal static string LastTabletActionText => _lastTabletActionText;

	internal static float LastTabletActionTime => _lastTabletActionTime;

	internal static void HandleHotkeys()
	{
		if (Time.frameCount != _lastHotkeyFrame)
		{
			_lastHotkeyFrame = Time.frameCount;
			CoopHandshake.Tick();
			bool refillPressed = Input.GetKeyDown(SmartStockOrderPlugin.TabletRefillKey.Value);
			bool minimumPressed = Input.GetKeyDown(SmartStockOrderPlugin.TabletMinimumKey.Value);
			if (refillPressed || minimumPressed)
			{
				if (NetworkCartUtil.InMultiplayer && !CoopHandshake.PeersMatch)
				{
					CoopHandshake.WarnBulkGateOnce();
				}
				else
				{
					if (refillPressed)
					{
						MarkTabletAction(OrderFull());
					}
					if (minimumPressed)
					{
						MarkTabletAction(OrderMinimum());
					}
				}
			}

			bool likelyOpen = _tabletVisible
				|| ((Object)(object)_activeScanner != (Object)null && _activeScanner.isOpened);
			if (!likelyOpen && !refillPressed && !minimumPressed)
			{
				SmartStockOrderHints.Sync(false);
				return;
			}

			bool showHints = IsEnabledForClick() && IsTabletActiveForShortcuts();
			SmartStockOrderHints.Sync(showHints);
		}
	}

	internal static void SetTabletActive(TabletDevice tablet, bool active)
	{
		if (active && (Object)(object)tablet != (Object)null)
		{
			_activeTablet = tablet;
		}
		else if (!active && (Object)(object)tablet != (Object)null && (Object)(object)_activeTablet == (Object)(object)tablet)
		{
			_tabletVisible = false;
		}
	}

	internal static void SetTabletVisible(TabletDevice tablet, bool visible)
	{
		if (!((Object)(object)tablet == (Object)null))
		{
			_activeTablet = tablet;
			_tabletVisible = visible;
		}
	}

	internal static void SetScannerVisible(ScannerDevice scanner, bool visible)
	{
		if (!((Object)(object)scanner == (Object)null))
		{
			_activeScanner = scanner;
			_tabletVisible = visible;
		}
	}

	internal static bool IsTabletActiveForShortcuts()
	{
		if ((Object)(object)_activeScanner != (Object)null && _activeScanner.isOpened)
		{
			return true;
		}

		TabletDevice val2 = _activeTablet;
		if ((Object)(object)val2 == (Object)null)
		{
			return false;
		}

		if (!((Behaviour)val2).enabled || !((Component)val2).gameObject.activeInHierarchy)
		{
			return false;
		}
		if (_tabletVisible && (Object)(object)val2.m_Tablet != (Object)null)
		{
			return ((Component)val2.m_Tablet).gameObject.activeInHierarchy;
		}
		return false;
	}

	internal static void HandleTabletKeys()
	{
		HandleHotkeys();
	}

	internal static void MarkTabletAction(string text)
	{
		_lastTabletActionText = text;
		_lastTabletActionTime = Time.unscaledTime;
		SmartStockOrderPlugin.LogSource.LogInfo((object)("Tablet shortcut: " + text));
	}

	internal static bool ShouldRemoveCartLimit()
	{
		if (SmartStockOrderPlugin.RemoveCartLimit != null)
		{
			return SmartStockOrderPlugin.RemoveCartLimit.Value;
		}
		return false;
	}

	internal static void ApplyCartLimitOverride(MarketShoppingCart cart)
	{
		if (!((Object)(object)cart == (Object)null) && ShouldRemoveCartLimit())
		{
			int num = Mathf.Max(51, SmartStockOrderPlugin.CartLimitOverride.Value);
			if (cart.m_MaxItemCount < num)
			{
				cart.m_MaxItemCount = num;
			}
		}
	}

	internal static void AddCartItemDirect(MarketShoppingCart cart, ItemQuantity item, SalesType salesType)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)cart == (Object)null) && item != null)
		{
			try
			{
				cart.AddProduct(item, salesType);
				return;
			}
			catch
			{
			}
			if (_addProductMethod == null)
			{
				_addProductMethod = AccessTools.Method(typeof(MarketShoppingCart), "AddProduct", (Type[])null, (Type[])null);
			}
			_addProductMethod.Invoke(cart, new object[2] { item, salesType });
		}
	}

	internal static void OrderFullFromButton()
	{
		OrderFull();
	}

	internal static void OrderMinimumFromButton()
	{
		OrderMinimum();
	}

	internal static string OrderFull()
	{
		if (!IsEnabledForClick())
		{
			return "Disabled";
		}
		MarketShoppingCart val = FindCart();
		if ((Object)(object)val == (Object)null)
		{
			SmartStockOrderPlugin.LogSource.LogWarning((object)"Auto order failed: open the market/computer cart first.");
			return "Open cart first";
		}
		Dictionary<int, ProductPlan> dictionary = BuildPlans();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		foreach (KeyValuePair<int, ProductPlan> item in dictionary)
		{
			int num = 0;
			ProductPlan value = item.Value;
			if (SmartStockOrderPlugin.IncludeRackShortage.Value)
			{
				num += value.RackMissingBoxes;
			}
			if (num > 0)
			{
				dictionary2[item.Key] = num;
			}
		}
		return AddOrderToCart(val, dictionary2, "full refill");
	}

	internal static string OrderMinimum()
	{
		if (!IsEnabledForClick())
		{
			return "Disabled";
		}
		MarketShoppingCart val = FindCart();
		if ((Object)(object)val == (Object)null)
		{
			SmartStockOrderPlugin.LogSource.LogWarning((object)"Auto order failed: open the market/computer cart first.");
			return "Open cart first";
		}
		Dictionary<int, ProductPlan> dictionary = BuildPlans();
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		foreach (KeyValuePair<int, ProductPlan> item in dictionary)
		{
			ProductPlan value = item.Value;
			bool flag = value.RackBoxCount <= 0;
			bool flag2 = value.DisplayProductCount <= 0;
			if (SmartStockOrderPlugin.MinimumUsesBoxStockOnly.Value ? flag : (flag || flag2))
			{
				dictionary2[item.Key] = 1;
			}
		}
		return AddOrderToCart(val, dictionary2, "minimum stock");
	}

	private static Dictionary<int, ProductPlan> BuildPlans()
	{
		Dictionary<int, ProductPlan> dictionary = new Dictionary<int, ProductPlan>();
		IDManager idManager = GetIdManager();
		EnsurePlanSceneCaches();
		DisplaySlot[] array = _displaySlotCache;
		if (array != null)
		{
			foreach (DisplaySlot val in array)
			{
				if (!((Object)(object)val == (Object)null))
				{
					int displayProductId = GetDisplayProductId(val);
					if (displayProductId >= 0)
					{
						ProductSO product = (val.HasProduct ? val.PeekProductSO() : FindProduct(idManager, displayProductId));
						ProductPlan plan = GetPlan(dictionary, displayProductId, product);
						int num = Mathf.Max(0, val.ProductCount);
						int displayCapacity = GetDisplayCapacity(product);
						plan.DisplayProductCount += num;
						plan.DisplayMissingProducts += Mathf.Max(0, displayCapacity - num);
						plan.DisplaySlotCount++;
					}
				}
			}
		}
		RackSlot[] array2 = _rackSlotCache;
		if (array2 != null)
		{
			foreach (RackSlot val2 in array2)
			{
				if ((Object)(object)val2 == (Object)null)
				{
					continue;
				}
				int rackProductId = GetRackProductId(val2);
				if (rackProductId >= 0)
				{
					ProductSO product2 = FindProduct(idManager, rackProductId);
					ProductPlan plan2 = GetPlan(dictionary, rackProductId, product2);
					int num2 = ((val2.Boxes != null) ? Mathf.Max(0, val2.Boxes.Count) : 0);
					plan2.RackBoxCount += num2;
					plan2.RackProductCount += Mathf.Max(0, val2.ProductCount);
					plan2.RackSlotCount++;
					if (!val2.Full)
					{
						int rackSlotTargetBoxes = GetRackSlotTargetBoxes(product2, idManager);
						plan2.RackMissingBoxes += Mathf.Max(0, rackSlotTargetBoxes - num2);
					}
				}
			}
		}
		Crate[] array3 = _crateCache;
		if (array3 != null)
		{
			foreach (Crate val3 in array3)
			{
				if (!((Object)(object)val3 == (Object)null) && val3.IsEnabled)
				{
					ProductSO crateProduct = GetCrateProduct(val3);
					if (!((Object)(object)crateProduct == (Object)null) && (int)crateProduct.ProductDisplayType == 2)
					{
						ProductPlan plan3 = GetPlan(dictionary, crateProduct.ID, crateProduct);
						int crateProductCount = GetCrateProductCount(val3, crateProduct.ID);
						int crateCapacity = GetCrateCapacity(val3, crateProduct);
						plan3.CrateProductCount += crateProductCount;
						plan3.CrateMissingProducts += Mathf.Max(0, crateCapacity - crateProductCount);
						plan3.CrateCount++;
					}
				}
			}
		}
		return dictionary;
	}

	private static void EnsurePlanSceneCaches()
	{
		if (_displaySlotCache != null
			&& _rackSlotCache != null
			&& _crateCache != null
			&& Time.unscaledTime < _planSceneCacheTime)
		{
			return;
		}

		_displaySlotCache = Object.FindObjectsOfType<DisplaySlot>();
		_rackSlotCache = Object.FindObjectsOfType<RackSlot>();
		_crateCache = Object.FindObjectsOfType<Crate>();
		_planSceneCacheTime = Time.unscaledTime + PlanSceneCacheTtl;
	}

	private static IDManager GetIdManager()
	{
		if ((Object)(object)_cachedIdManager != (Object)null)
		{
			return _cachedIdManager;
		}

		if (Time.unscaledTime < _nextIdManagerLookup)
		{
			return null;
		}

		_cachedIdManager = Object.FindObjectOfType<IDManager>();
		_nextIdManagerLookup = Time.unscaledTime + 2f;
		return _cachedIdManager;
	}

	private static bool IsEnabledForClick()
	{
		return true;
	}

	private static ProductPlan GetPlan(Dictionary<int, ProductPlan> plans, int productId, ProductSO product)
	{
		if (!plans.TryGetValue(productId, out var value))
		{
			value = new ProductPlan();
			value.ProductId = productId;
			value.Product = product;
			value.UnitsPerBox = GetUnitsPerBox(product);
			plans.Add(productId, value);
		}
		else if ((Object)(object)value.Product == (Object)null && (Object)(object)product != (Object)null)
		{
			value.Product = product;
			value.UnitsPerBox = GetUnitsPerBox(product);
		}
		return value;
	}

	private static int GetDisplayProductId(DisplaySlot slot)
	{
		if (slot.HasProduct)
		{
			return slot.ProductID;
		}
		if (slot.Data != null && slot.Data.HasLabel)
		{
			return slot.Data.FirstItemID;
		}
		return -1;
	}

	private static int GetRackProductId(RackSlot slot)
	{
		if (slot.Data != null && slot.Data.ProductID >= 0)
		{
			return slot.Data.ProductID;
		}
		if (slot.HasBox)
		{
			Box val = slot.PeakBoxFromRack();
			if ((Object)(object)val != (Object)null && val.Data != null)
			{
				return val.Data.ProductID;
			}
		}
		return -1;
	}

	private static int GetRackSlotTargetBoxes(ProductSO product, IDManager idManager)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)product == (Object)null)
		{
			return 1;
		}
		if ((int)product.ProductDisplayType == 2)
		{
			return 4;
		}
		try
		{
			if ((Object)(object)idManager != (Object)null && product.GridLayoutInBox != null && idManager.Boxes != null)
			{
				BoxSize boxSize = product.GridLayoutInBox.boxSize;
				for (int i = 0; i < idManager.Boxes.Count; i++)
				{
					BoxSO val = idManager.Boxes[i];
					if ((Object)(object)val != (Object)null && val.BoxSize == boxSize && val.GridLayout != null)
					{
						return Mathf.Max(1, val.GridLayout.boxCount);
					}
				}
			}
		}
		catch
		{
		}
		return 1;
	}

	private static ProductSO GetCrateProduct(Crate crate)
	{
		if ((Object)(object)crate == (Object)null)
		{
			return null;
		}
		try
		{
			if (crate.Products != null && crate.Products.Count > 0)
			{
				Product val = crate.Products[0];
				if ((Object)(object)val != (Object)null)
				{
					return val.ProductSO;
				}
			}
		}
		catch
		{
		}
		try
		{
			if (crate.m_Product != null && crate.m_Product.Count > 0)
			{
				Product val2 = crate.m_Product[0];
				if ((Object)(object)val2 != (Object)null)
				{
					return val2.ProductSO;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static int GetCrateProductCount(Crate crate, int productId)
	{
		if ((Object)(object)crate == (Object)null || productId < 0)
		{
			return 0;
		}
		int num = 0;
		try
		{
			if (crate.Products != null)
			{
				for (int i = 0; i < crate.Products.Count; i++)
				{
					Product val = crate.Products[i];
					if ((Object)(object)val != (Object)null && (Object)(object)val.ProductSO != (Object)null && val.ProductSO.ID == productId)
					{
						num++;
					}
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static int GetCrateCapacity(Crate crate, ProductSO product)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)crate == (Object)null || (Object)(object)product == (Object)null)
		{
			return GetUnitsPerBox(product);
		}
		Vector3Int size = crate.m_Size;
		Vector3Int itemGridSizeInCrate = product.ItemGridSizeInCrate;
		int num = ((itemGridSizeInCrate.x > 0) ? (size.x / itemGridSizeInCrate.x) : 0);
		int num2 = ((itemGridSizeInCrate.y > 0) ? (size.y / itemGridSizeInCrate.y) : 0);
		int num3 = ((itemGridSizeInCrate.z > 0) ? (size.z / itemGridSizeInCrate.z) : 0);
		int num4 = num * num2 * num3;
		return Mathf.Max(GetUnitsPerBox(product), num4);
	}

	private static ProductSO FindProduct(IDManager idManager, int productId)
	{
		if ((Object)(object)idManager == (Object)null || productId < 0)
		{
			return null;
		}
		try
		{
			return idManager.ProductSO(productId);
		}
		catch (Exception ex)
		{
			SmartStockOrderPlugin.LogSource.LogDebug((object)("Could not resolve ProductSO " + productId + ": " + ex.Message));
			return null;
		}
	}

	private static int GetUnitsPerBox(ProductSO product)
	{
		if ((Object)(object)product == (Object)null)
		{
			return 1;
		}
		return Mathf.Max(1, product.ProductAmountOnPurchase);
	}

	private static int GetDisplayCapacity(ProductSO product)
	{
		if ((Object)(object)product == (Object)null || product.GridLayoutInStorage == null)
		{
			return GetUnitsPerBox(product);
		}
		return Mathf.Max(GetUnitsPerBox(product), product.GridLayoutInStorage.productCount);
	}

	private static MarketShoppingCart FindCart()
	{
		TabletDevice tablet;
		MarketShoppingCart val = FindTabletCart(out tablet);
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		MarketShoppingCart val2 = null;
		try
		{
			val2 = Singleton<MarketShoppingCart>.Instance;
		}
		catch
		{
		}
		if ((Object)(object)val2 != (Object)null)
		{
			return val2;
		}
		ProductViewer val3 = Object.FindObjectOfType<ProductViewer>();
		if ((Object)(object)val3 != (Object)null && (Object)(object)val3.ShoppingCart != (Object)null)
		{
			return val3.ShoppingCart;
		}
		MarketShoppingCart val4 = Object.FindObjectOfType<MarketShoppingCart>();
		if ((Object)(object)val4 != (Object)null)
		{
			return val4;
		}
		MarketShoppingCart[] array = Resources.FindObjectsOfTypeAll<MarketShoppingCart>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				return array[i];
			}
		}
		return null;
	}

	private static MarketShoppingCart FindTabletCart(out TabletDevice tablet)
	{
		tablet = (((Object)(object)_activeTablet != (Object)null) ? _activeTablet : FindTabletDevice());
		if ((Object)(object)tablet == (Object)null)
		{
			return null;
		}
		try
		{
			return tablet.m_MarketShoppingCart;
		}
		catch
		{
			return null;
		}
	}

	private static TabletDevice FindTabletDevice()
	{
		TabletDevice val = Object.FindObjectOfType<TabletDevice>();
		if ((Object)(object)val != (Object)null)
		{
			return val;
		}
		TabletDevice[] array = Resources.FindObjectsOfTypeAll<TabletDevice>();
		for (int i = 0; i < array.Length; i++)
		{
			if ((Object)(object)array[i] != (Object)null)
			{
				return array[i];
			}
		}
		return null;
	}

	private static string AddOrderToCart(MarketShoppingCart cart, Dictionary<int, int> order, string reason)
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		if (order.Count == 0)
		{
			SmartStockOrderPlugin.LogSource.LogInfo((object)("Auto order " + reason + ": nothing needed."));
			return "Nothing needed";
		}
		IDManager idManager = GetIdManager();
		FindTabletCart(out var tablet);
		int num = Mathf.Max(1, SmartStockOrderPlugin.MaxBoxesPerRun.Value);
		int num2 = 0;
		int num3 = 0;
		foreach (KeyValuePair<int, int> item in order)
		{
			ProductSO val = FindProduct(idManager, item.Key);
			if ((Object)(object)val == (Object)null || val.IsHiddenInMarketApp)
			{
				SmartStockOrderPlugin.LogSource.LogInfo((object)("Auto order skipped product " + item.Key + ": not available in market."));
				continue;
			}
			int num4 = Mathf.Min(Mathf.Max(0, item.Value), num - num2);
			if (num4 <= 0)
			{
				break;
			}
			try
			{
				ItemQuantity val2 = new ItemQuantity(item.Key, val.BasePrice);
				val2.FirstItemCount = num4;
				NetworkCartUtil.TryAddProduct(cart, val2, (SalesType)0);
				RefreshTabletCart(tablet);
				num2 += num4;
				num3 += num4;
			}
			catch (Exception ex)
			{
				SmartStockOrderPlugin.LogSource.LogWarning((object)("Auto order failed product " + item.Key + ": " + ex.Message));
			}
			if (num2 < num)
			{
				continue;
			}
			break;
		}
		SmartStockOrderPlugin.LogSource.LogInfo((object)("Auto order " + reason + ": added " + num3 + " box(es) to cart, requested cap " + num + "."));
		if (num3 <= 0)
		{
			return "Nothing added";
		}
		return "Added " + num3 + " boxes";
	}

	private static void RefreshTabletCart(TabletDevice tablet)
	{
		if ((Object)(object)tablet == (Object)null)
		{
			return;
		}
		try
		{
			tablet.CreateList();
		}
		catch (Exception ex)
		{
			SmartStockOrderPlugin.LogSource.LogDebug((object)("Tablet CreateList failed: " + ex.Message));
		}
		try
		{
			tablet.RefreshMoneyScreen();
		}
		catch (Exception ex2)
		{
			SmartStockOrderPlugin.LogSource.LogDebug((object)("Tablet RefreshMoneyScreen failed: " + ex2.Message));
		}
	}
}