using System;
using System.Collections.Generic;
using System.Linq;
using MyBox;
using UnityEngine;

namespace EmployeeTraining;

public static class Utils
{
	public static T FindResourceByName<T>(string name) where T : Object
	{
		return Il2CppHelpers.FindResourceByName<T>(name);
	}

	public static string ToBoxInfo(this Box box)
	{
		if (Plugin.Instance.Settings.RestockerLog)
		{
			if (box.HasProducts)
			{
				return string.Format("[{0} x{1}]", box.Product?.ProductName ?? "UNKNOWN", box.Data?.ProductCount ?? 0);
			}
			return "[EMPTY]";
		}
		return "";
	}

	public static string ToBoxStackInfo(this IEnumerable<Box> list)
	{
		return Il2CppHelpers.Join(list.Select((Box b) => b.ToBoxInfo()), "");
	}

	public static string ToRackInfo(this RackSlot rack)
	{
		if (Plugin.Instance.Settings.RestockerLog)
		{
			if (rack.HasProduct)
			{
				return $"<{Singleton<IDManager>.Instance.ProductSO(rack.Data.ProductID).name} x{rack.Data.TotalProductCount}>";
			}
			return "<EMPTY>";
		}
		return "";
	}
}
