using System;

namespace CashierTraining;

[Serializable]
public class CashierSkillData
{
	public int Id;

	public int Exp = 0;

	public int Grade = 0;

	public bool IsGaugeDisplayed = true;
}
