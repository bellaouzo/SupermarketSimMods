using UnityEngine;
using __Project__.Scripts.FloorPaintSystem;

namespace MultiBoxCarry;

internal static class BoxDisplay
{
	internal static string GetLabel(IQueuableBox box)
	{
		if (box == null)
		{
			return "Box";
		}

		if (box is BoxAdapter boxAdapter)
		{
			Box productBox = boxAdapter.GetBox();
			if ((Object)(object)productBox == (Object)null)
			{
				return "Product Box";
			}

			ProductSO product = productBox.Product;
			string name = GetProductName(product);
			int count = productBox.ProductCount;
			int max = productBox.MaxProductCount;
			if (string.IsNullOrEmpty(name))
			{
				return count > 0 ? $"Box ({count}/{max})" : "Empty Box";
			}

			if (count <= 0)
			{
				return $"{name} (empty)";
			}

			return $"{name} ({count}/{max})";
		}

		if (box is FurnitureBoxAdapter furnitureAdapter)
		{
			FurnitureBox furnitureBox = furnitureAdapter.GetFurnitureBox();
			FurnitureBoxData data = furnitureBox != null ? furnitureBox.Data : null;
			if (data != null)
			{
				FurnitureSO furniture = data.Furniture;
				if ((Object)(object)furniture != (Object)null)
				{
					string furnitureName = furniture.FurnitureName;
					if (!string.IsNullOrEmpty(furnitureName))
					{
						return furnitureName;
					}
				}
			}

			return "Furniture Box";
		}

		if (box is FloorBoxAdapter)
		{
			return "Floor Box";
		}

		return "Box";
	}

	private static string GetProductName(ProductSO product)
	{
		if ((Object)(object)product == (Object)null)
		{
			return null;
		}

		string temp = product.TempProductName;
		if (!string.IsNullOrEmpty(temp))
		{
			return temp;
		}

		return product.ProductName;
	}
}
