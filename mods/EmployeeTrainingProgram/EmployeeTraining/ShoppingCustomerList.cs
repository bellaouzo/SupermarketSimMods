using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace EmployeeTraining;

public class ShoppingCustomerList : MonoBehaviour
{
	public ShoppingCustomerList(IntPtr ptr)
		: base(ptr)
	{
	}

	public static ShoppingCustomerList Instance { get; private set; }

	[HideFromIl2Cpp]
	public List<Customer> CustomersInShopping { get; private set; }

	public void Awake()
	{
		Instance = this;
		CustomersInShopping = new List<Customer>();
	}

	public void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void StartShopping(Customer c)
	{
		CustomersInShopping.Add(c);
	}

	public void FinishShopping(Customer c)
	{
		CustomersInShopping.Remove(c);
	}

	public void ClearCustomers()
	{
		CustomersInShopping.Clear();
	}
}
