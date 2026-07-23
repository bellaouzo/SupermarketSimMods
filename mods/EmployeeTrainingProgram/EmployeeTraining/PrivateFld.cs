using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivateFld<T>
{
	private readonly FieldInfo fld;

	private readonly string name;

	public object Instance { private get; set; }

	public T Value
	{
		get
		{
			return (T)fld.GetValue(Instance);
		}
		set
		{
			fld.SetValue(Instance, value);
		}
	}

	public PrivateFld(Type type, string name)
	{
		this.name = name;
		try
		{
			fld = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to get field " + name + " from " + type.Name, innerException);
		}
	}
}
