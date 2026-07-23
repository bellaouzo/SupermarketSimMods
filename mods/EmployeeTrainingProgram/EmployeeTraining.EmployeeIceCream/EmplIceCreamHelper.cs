using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeIceCream;

public class EmplIceCreamHelper : Employee<IceCreamHelper>
{
	public override int ID => Instance.ID;
}
