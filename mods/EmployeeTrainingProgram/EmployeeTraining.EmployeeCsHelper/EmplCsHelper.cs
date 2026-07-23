using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCsHelper;

public class EmplCsHelper : Employee<CustomerHelper>
{
	public override int ID => Instance.CustomerHelperID;
}
