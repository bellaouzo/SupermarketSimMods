using __Project__.Scripts.Janitor;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeJanitor;

public class EmplJanitor : Employee<Janitor>
{
	public override int ID => Instance.JanitorID;
}
