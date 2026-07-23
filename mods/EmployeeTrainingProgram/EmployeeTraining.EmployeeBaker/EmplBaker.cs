using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeBaker;

public class EmplBaker : Employee<Baker>
{
	public override int ID => Instance.BakerID;
}
