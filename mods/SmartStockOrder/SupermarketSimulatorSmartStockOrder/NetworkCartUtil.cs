using System;
using Photon.Pun;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SupermarketSimulatorSmartStockOrder;

internal static class NetworkCartUtil
{
	internal static bool InMultiplayer
	{
		get
		{
			try
			{
				return PhotonNetwork.InRoom;
			}
			catch
			{
				return false;
			}
		}
	}

	internal static bool TryAddProduct(MarketShoppingCart cart, ItemQuantity item, SalesType salesType)
	{
		if ((Object)(object)cart == (Object)null || item == null)
		{
			return false;
		}

		if (InMultiplayer)
		{
			NetworkMarketShoppingCart network = ((Component)cart).GetComponent<NetworkMarketShoppingCart>()
				?? ((Component)cart).GetComponentInParent<NetworkMarketShoppingCart>()
				?? Object.FindObjectOfType<NetworkMarketShoppingCart>();
			if ((Object)(object)network == (Object)null)
			{
				SmartStockOrderPlugin.LogSource.LogWarning((object)"Network cart missing; refusing local TryAddProduct in multiplayer.");
				return false;
			}

			try
			{
				network.TryAddProduct_Request(item, salesType);
				return true;
			}
			catch (Exception ex)
			{
				SmartStockOrderPlugin.LogSource.LogWarning((object)("Network cart add failed; refusing local fallback: " + ex.Message));
				return false;
			}
		}

		cart.TryAddProduct(item, salesType);
		return true;
	}
}
