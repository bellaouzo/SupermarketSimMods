using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeBaker;

public class BakerAppTab : EmployeeAppTab<BakerTrainingProgressItem, BakerSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Bakers Tab Button", UIHelper.IconBaker, "Bakers");
	}

	void IEmployeeAppTab.CreateStatusPanel(GameObject panelObj, GameObject panelTmpl)
	{
		CreateStatusPanel(panelObj, panelTmpl);
	}

	public void ComposeStatusPanel()
	{
		ComposeStatusPanel(UIHelper.IconBaker, "Baker Name", new Vector2(71f, 25f), 4, new DetailParam[4]
		{
			new DetailParam
			{
				Name = "Rapidity",
				LabelKey = "Walk Speed",
				ValueKey = "Speed",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParam
			{
				Name = "Place Speed",
				LabelKey = "Place Speed",
				ValueKey = "Place Speed Sec.",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParam
			{
				Name = "Box Handling",
				LabelKey = "Box Handling",
				ValueKey = "Box Handling Sec.",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParamWage()
		}, new Color(0.55f, 0.38f, 0.22f, 0.94f), new Color(0.78f, 0.55f, 0.28f, 1f), new Color(0.95f, 0.72f, 0.42f, 1f), new Color(0.45f, 0.3f, 0.16f, 1f), new Color(0.36f, 0.24f, 0.14f, 1f));
	}

	void IEmployeeAppTab.CreateTabScreen(GameObject baseTabObj, GameObject tabsObj)
	{
		CreateTabScreen(baseTabObj, tabsObj);
	}

	public void ComposeTabScreen()
	{
		ComposeTabScreen("Bakers Tab", "bakers", (Color32)(new Color32((byte)255, (byte)236, (byte)210, byte.MaxValue)));
		ComposeNoEmployee("No Bakers", "NO BAKERS HIRED", (Color32)(new Color32((byte)230, (byte)190, (byte)140, byte.MaxValue)));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Bakers Tab Button", "bakers");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Baker Status Panel", (BakerSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		Il2CppSystem.Collections.Generic.List<int> hired = Singleton<EmployeeManager>.Instance?.m_BakersData;
		List<int> value = new List<int>();
		if (hired != null)
		{
			foreach (int id in hired)
			{
				value.Add(id);
			}
		}
		DeleteEmployee((BakerSkill)skill, value);
	}
}
