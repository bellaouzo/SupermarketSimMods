using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using HarmonyLib;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeSecurity;

public class SecurityAppTab : EmployeeAppTab<SecurityTrainingProgressItem, SecuritySkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Security Guard Tab Button", UIHelper.IconSecurity, "Security Guards");
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
		ComposeStatusPanel(UIHelper.IconSecurity, "Security Guard Name", bgColorPanel: (Color?)new Color(0.6039f, 0.5946f, 0.349f, 0.9412f), bgColorIntr: (Color?)(Color32)(new Color32((byte)174, (byte)200, (byte)103, byte.MaxValue)), bgColorExp: (Color?)new Color(0.82f, 0.9f, 0.45f, 1f), bgColorRmSlider: (Color?)(Color32)(new Color32((byte)97, (byte)107, (byte)64, byte.MaxValue)), bgColorRmSeal: (Color?)(Color32)(new Color32((byte)83, (byte)87, (byte)48, byte.MaxValue)), detailCellSize: new Vector2(95f, 25f), detailParamCount: 3, detailParams: new DetailParam[3]
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
		ComposeTabScreen("Security Guard Tab", "guards", (Color32)(new Color32((byte)239, byte.MaxValue, (byte)226, byte.MaxValue)));
		ComposeNoEmployee("No Security Guards", "NO SECURITY GUARDS HIRED", (Color32)(new Color32((byte)196, (byte)210, (byte)158, byte.MaxValue)));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Security Guard Tab Button", "guards");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Security Guard Status Panel", (SecuritySkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		List<int> value = Traverse.Create((object)Singleton<EmployeeManager>.Instance).Field<List<int>>("m_SecurityGuardsData").Value;
		DeleteEmployee((SecuritySkill)skill, value);
	}
}
