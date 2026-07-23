using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeSecurity;

public class EmplSecurity : Employee<SecurityGuard>
{
	public override int ID => Instance.ID;
}
