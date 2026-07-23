using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Il2CppIEnumerator = Il2CppSystem.Collections.IEnumerator;

namespace EmployeeTraining;

internal static class Il2CppHelpers
{
	public static Il2CppIEnumerator Wrap(IEnumerator enumerator)
	{
		return enumerator.WrapToIl2Cpp();
	}

	public static GameObject NewGameObject(string name, params Type[] componentTypes)
	{
		GameObject go = new GameObject(name);
		foreach (Type componentType in componentTypes)
		{
			go.AddComponent(Il2CppType.From(componentType));
		}
		return go;
	}

	public static T FindResourceByName<T>(string name) where T : Object
	{
		Il2CppReferenceArray<Object> all = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
		for (int i = 0; i < all.Count; i++)
		{
			Object obj = all[i];
			if (obj != null && obj.name == name)
			{
				return obj.TryCast<T>();
			}
		}
		return null;
	}

	public static string Join<T>(System.Collections.Generic.IEnumerable<T> items, string separator = "")
	{
		return string.Join(separator, items);
	}

	public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(System.Collections.Generic.IEnumerable<T> items)
	{
		var list = new Il2CppSystem.Collections.Generic.List<T>();
		foreach (T item in items)
		{
			list.Add(item);
		}
		return list;
	}

	public static System.Collections.Generic.List<T> ToSystemList<T>(Il2CppSystem.Collections.Generic.List<T> items)
	{
		var list = new System.Collections.Generic.List<T>(items.Count);
		foreach (T item in items)
		{
			list.Add(item);
		}
		return list;
	}

	public static System.Collections.Generic.List<int> KeysToList(Il2CppSystem.Collections.Generic.Dictionary<int, int> dict)
	{
		var list = new System.Collections.Generic.List<int>();
		foreach (Il2CppSystem.Collections.Generic.KeyValuePair<int, int> pair in dict)
		{
			list.Add(pair.Key);
		}
		return list;
	}
}
