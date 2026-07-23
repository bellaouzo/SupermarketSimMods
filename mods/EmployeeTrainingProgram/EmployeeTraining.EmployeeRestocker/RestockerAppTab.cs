using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeRestocker;

public class RestockerAppTab : EmployeeAppTab<RestockerTrainingProgressItem, RestockerSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Restockers Tab Button", UIHelper.IconRestocker, "Restockers");
	}

	void IEmployeeAppTab.CreateStatusPanel(GameObject panelObj, GameObject panelTmpl)
	{
		CreateStatusPanel(panelObj, panelTmpl);
	}

	public void ComposeStatusPanel()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		ComposeStatusPanel(UIHelper.IconRestocker, "Restocker Name", bgColorPanel: (Color?)new Color(0.34f, 0.3f, 0.42f, 0.95f), bgColorIntr: (Color?)new Color(0.45f, 0.42f, 0.58f, 1f), bgColorExp: (Color?)new Color(0.72f, 0.66f, 0.92f, 1f), bgColorRmSlider: (Color?)new Color(0.22f, 0.24f, 0.36f, 1f), bgColorRmSeal: (Color?)new Color(0.18f, 0.19f, 0.28f, 1f), detailCellSize: new Vector2(71f, 25f), detailParamCount: 4, detailParams: new DetailParam[4]
		{
			new DetailParam
			{
				Name = "Rapidity",
				LabelKey = "Rapidity",
				ValueKey = "Speed",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParam
			{
				Name = "Capacity",
				LabelKey = "Capacity",
				ValueKey = "Weight/Height",
				ValueArgs = new string[2] { "0", "0" }
			},
			new DetailParam
			{
				Name = "Dexterity",
				LabelKey = "Dexterity",
				ValueKey = "Percentage",
				ValueArgs = new string[1] { "0" }
			},
			new DetailParamWage()
		});
	}

	void IEmployeeAppTab.CreateTabScreen(GameObject baseTabObj, GameObject tabsObj)
	{
		CreateTabScreen(baseTabObj, tabsObj);
	}

	public void ComposeTabScreen()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		ComposeTabScreen("Restockers Tab", "restockers", (Color32)(new Color32((byte)224, (byte)218, byte.MaxValue, byte.MaxValue)));
		ComposeNoEmployee("No Restockers", "NO RESTOCKERS HIRED", (Color32)(new Color32((byte)192, (byte)177, byte.MaxValue, byte.MaxValue)));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Restockers Tab Button", "restockers");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Restocker Status Panel", (RestockerSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		Il2CppSystem.Collections.Generic.List<int> hired = Singleton<EmployeeManager>.Instance?.m_RestockersData;
		List<int> value = new List<int>();
		if (hired != null)
		{
			foreach (int id in hired)
			{
				value.Add(id);
			}
		}
		DeleteEmployee((RestockerSkill)skill, value);
	}
}
