using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeIceCream;

public class IceCreamHelperAppTab : EmployeeAppTab<IceCreamHelperTrainingProgressItem, IceCreamHelperSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Ice Cream Helpers Tab Button", UIHelper.IconIceCream, "Ice Cream Helpers");
	}

	void IEmployeeAppTab.CreateStatusPanel(GameObject panelObj, GameObject panelTmpl)
	{
		CreateStatusPanel(panelObj, panelTmpl);
	}

	public void ComposeStatusPanel()
	{
		ComposeStatusPanel(UIHelper.IconIceCream, "Ice Cream Helper Name", new Vector2(95f, 25f), 3, new DetailParam[3]
		{
			new DetailParam
			{
				Name = "Serve Speed",
				LabelKey = "Serve Speed",
				ValueKey = "Serve Speed Sec.",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParam
			{
				Name = "Serve Rate",
				LabelKey = "Serve Rate",
				ValueKey = "Serves Per Minute",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParamWage()
		}, new Color(0.2f, 0.48f, 0.5f, 0.94f), new Color(0.35f, 0.72f, 0.7f, 1f), new Color(0.55f, 0.9f, 0.88f, 1f), new Color(0.18f, 0.4f, 0.42f, 1f), new Color(0.14f, 0.32f, 0.34f, 1f));
	}

	void IEmployeeAppTab.CreateTabScreen(GameObject baseTabObj, GameObject tabsObj)
	{
		CreateTabScreen(baseTabObj, tabsObj);
	}

	public void ComposeTabScreen()
	{
		ComposeTabScreen("Ice Cream Helpers Tab", "icecreamhelpers", (Color32)(new Color32((byte)230, (byte)250, (byte)248, byte.MaxValue)));
		ComposeNoEmployee("No Ice Cream Helpers", "NO ICE CREAM HELPERS HIRED", (Color32)(new Color32((byte)170, (byte)220, (byte)215, byte.MaxValue)));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Ice Cream Helpers Tab Button", "icecreamhelpers");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Ice Cream Helper Status Panel", (IceCreamHelperSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		Il2CppSystem.Collections.Generic.List<int> hired = Singleton<EmployeeManager>.Instance?.IceCreamHelpersData
			?? Singleton<EmployeeManager>.Instance?.m_IceCreamHelpersData;
		List<int> value = new List<int>();
		if (hired != null)
		{
			foreach (int id in hired)
			{
				value.Add(id);
			}
		}
		DeleteEmployee((IceCreamHelperSkill)skill, value);
	}
}
