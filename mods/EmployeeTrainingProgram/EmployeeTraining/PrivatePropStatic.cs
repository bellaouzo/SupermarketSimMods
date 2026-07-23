using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivatePropStatic<V>
{
	internal readonly PropertyInfo prop;

	internal readonly string name;

	public PrivatePropStatic(Type type, string name, BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic)
	{
		this.name = name;
		try
		{
			prop = type.GetProperty(name, flags);
			if (prop == null)
			{
				throw new Exception("Property " + name + " not found in " + type.Name);
			}
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to get property " + name + " from " + type.Name, innerException);
		}
	}

	public V Get(object instance)
	{
		if (prop.CanRead)
		{
			return (V)prop.GetValue(instance);
		}
		throw new Exception("Unable to get value from " + name);
	}

	public void Set(object instance, V value)
	{
		if (prop.CanWrite)
		{
			prop.SetValue(instance, value);
			return;
		}
		throw new Exception("Unable to set value to " + name);
	}
}
