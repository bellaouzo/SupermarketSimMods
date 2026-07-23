using System;
using UnityEngine;

namespace EmployeeTraining.Employee;

public interface IEmployeeSkill
{
	int Id { get; }

	string JobName { get; }

	int Exp { get; }

	int TotalExp { get; }

	int Lvl { get; }

	Grade Grade { get; }

	float Wage { get; }

	GameObject TrainingStatusPanelObj { get; set; }

	GameObject ExpGaugeObj { get; set; }

	bool IsGaugeDisplayed { get; set; }

	Action<int, bool> OnExpChanged { get; set; }

	Action<bool> OnLevelChanged { get; set; }

	float GetWage(Grade g);

	bool IsAssigned();

	void AddExp(int exp);

	void UpdateStatus(bool init);

	void UnlockGrade();

	int? GetExpForNext();

	int? GetTotalExpForNext();

	bool IsUnlockNeeded();

	float? GetCostToLevelup();

	float? GetCostToUpgrade();

	string GetExpDisplay();

	void TrainToLevelup();

	void Setup();

	ExpRange GetExpRangeOfGrade(Grade g);
}
