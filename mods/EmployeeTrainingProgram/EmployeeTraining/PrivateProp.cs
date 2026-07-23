using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivateProp<T>
{
	private readonly PropertyInfo prop;

	private readonly string name;

	public object Instance { private get; set; }

	public T Value
	{
		get
		{
			if (prop.CanRead)
			{
				return (T)prop.GetValue(Instance);
			}
			throw new Exception("Unable to get value from " + name);
		}
		set
		{
			if (prop.CanWrite)
			{
				prop.SetValue(Instance, value);
				return;
			}
			throw new Exception("Unable to set value to " + name);
		}
	}

	public PrivateProp(Type type, string name)
	{
		this.name = name;
		prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
	}
}
