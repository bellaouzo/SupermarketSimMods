using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCashier;

public struct CashierSkillTier : ISkillTier
{
	public int Lvl { get; set; }

	public int Exp { get; set; }

	public float IntervalMin { get; set; }

	public float IntervalMax { get; set; }

	public float Payment { get; set; }
}
