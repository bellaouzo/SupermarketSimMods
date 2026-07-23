using __Project__.Scripts.Janitor;
using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using HarmonyLib;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeJanitor;

public class JanitorAppTab : EmployeeAppTab<JanitorTrainingProgressItem, JanitorSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Janitor Tab Button", UIHelper.IconJanitor, "Janitors");
	}

	void IEmployeeAppTab.CreateStatusPanel(GameObject panelObj, GameObject panelTmpl)
	{
		CreateStatusPanel(panelObj, panelTmpl);
	}

	public void ComposeStatusPanel()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		ComposeStatusPanel(UIHelper.IconJanitor, "Janitor Name", bgColorPanel: (Color?)new Color(0.349f, 0.6039f, 0.549f, 0.9412f), bgColorIntr: (Color?)(Color32)(new Color32((byte)68, (byte)200, (byte)182, byte.MaxValue)), bgColorExp: (Color?)new Color(0.45f, 0.88f, 0.8f, 1f), bgColorRmSlider: (Color?)(Color32)(new Color32((byte)33, (byte)108, (byte)99, byte.MaxValue)), bgColorRmSeal: (Color?)(Color32)(new Color32((byte)64, (byte)95, (byte)89, byte.MaxValue)), detailCellSize: new Vector2(95f, 25f), detailParamCount: 3, detailParams: new DetailParam[3]
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
		ComposeTabScreen("Janitor Tab", "janitors", (Color32)(new Color32((byte)228, byte.MaxValue, (byte)251, byte.MaxValue)));
		ComposeNoEmployee("No Janitors", "NO JANITORS HIRED", (Color32)(new Color32((byte)150, (byte)212, (byte)206, byte.MaxValue)));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Janitor Tab Button", "janitors");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Janitor Status Panel", (JanitorSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		List<int> value = Traverse.Create((object)Singleton<EmployeeManager>.Instance).Field<List<int>>("m_JanitorsData").Value;
		DeleteEmployee((JanitorSkill)skill, value);
	}
}
