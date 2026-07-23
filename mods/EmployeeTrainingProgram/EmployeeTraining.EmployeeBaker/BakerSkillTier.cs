using System;
using EmployeeTraining.Employee;

namespace EmployeeTraining.EmployeeBaker;

public struct BakerSkillTier : ISkillTier
{
	public int Lvl { get; set; }

	public int Exp { get; set; }

	public float Rapidity { get; set; }

	public int Dexterity { get; set; }

	public int CappedDexterity => Math.Min(Dexterity, 100);
}
