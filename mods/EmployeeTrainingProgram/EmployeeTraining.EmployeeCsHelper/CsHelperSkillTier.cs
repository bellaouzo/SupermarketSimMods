using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeCsHelper;

public struct CsHelperSkillTier : ISkillTier
{
	public int Lvl { get; set; }

	public int Exp { get; set; }

	public float IntervalMin { get; set; }

	public float IntervalMax { get; set; }

	public float Rapidity { get; set; }
}
