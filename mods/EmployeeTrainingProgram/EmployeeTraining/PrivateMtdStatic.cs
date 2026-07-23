using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivateMtdStatic
{
	internal readonly MethodInfo mtd;

	internal readonly string name;

	public PrivateMtdStatic(Type type, string name, params Type[] args)
	{
		this.name = name;
		try
		{
			mtd = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, args, null);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to get method " + name + " from " + type.Name, innerException);
		}
	}

	public void Invoke(object instance, params object[] args)
	{
		try
		{
			mtd.Invoke(instance, args);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to invoke " + mtd.Name + " from " + mtd.GetType().Name, innerException);
		}
	}
}
public class PrivateMtdStatic<T> : PrivateMtdStatic
{
	public PrivateMtdStatic(Type type, string name, params Type[] args)
		: base(type, name, args)
	{
	}

	public new T Invoke(object instance, params object[] args)
	{
		try
		{
			return (T)mtd.Invoke(instance, args);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to invoke " + mtd.Name + " from " + mtd.GetType().Name, innerException);
		}
	}
}
