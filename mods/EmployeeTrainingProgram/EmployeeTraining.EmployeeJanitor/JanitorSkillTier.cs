using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeJanitor;

public struct JanitorSkillTier : ISkillTier
{
	public int Lvl { get; set; }

	public int Exp { get; set; }

	public float Rapidity { get; set; }

	public int Dexterity { get; set; }
}
