using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivateFldStatic<T>
{
	private readonly FieldInfo fld;

	private readonly string name;

	public PrivateFldStatic(Type type, string name)
	{
		this.name = name;
		try
		{
			fld = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to get field " + name + " from " + typeof(T).Name, innerException);
		}
	}

	public T GetValue(object instance)
	{
		return (T)fld.GetValue(instance);
	}

	public void SetValue(object instance, T value)
	{
		fld.SetValue(instance, value);
	}
}
