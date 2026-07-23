using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCashier;

public class EmplCashier : Employee<Cashier>
{
	public override int ID => Instance.CashierID;
}
