using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeSecurity;

public struct SecuritySkillTier : ISkillTier
{
	public int Lvl { get; set; }

	public int Exp { get; set; }

	public float Rapidity { get; set; }

	public int Dexterity { get; set; }
}
