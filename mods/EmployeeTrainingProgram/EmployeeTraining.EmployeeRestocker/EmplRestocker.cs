using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeRestocker;

public class EmplRestocker : Employee<Restocker>
{
	public override int ID => Instance.RestockerID;
}
