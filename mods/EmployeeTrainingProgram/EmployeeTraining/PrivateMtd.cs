using System;
using System.Reflection;

namespace EmployeeTraining;

public class PrivateMtd
{
	internal readonly MethodInfo mtd;

	internal readonly string name;

	public object Instance { internal get; set; }

	public PrivateMtd(Type type, string name, params Type[] args)
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

	public void Invoke(params object[] args)
	{
		try
		{
			mtd.Invoke(Instance, args);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to invoke " + mtd.Name + " from " + mtd.GetType().Name, innerException);
		}
	}
}
public class PrivateMtd<T> : PrivateMtd
{
	public PrivateMtd(Type type, string name, params Type[] args)
		: base(type, name, args)
	{
	}

	public new T Invoke(params object[] args)
	{
		try
		{
			return (T)mtd.Invoke(base.Instance, args);
		}
		catch (Exception innerException)
		{
			throw new Exception("Failed to invoke " + mtd.Name + " from " + mtd.GetType().Name, innerException);
		}
	}
}
