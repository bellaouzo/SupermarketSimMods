using System.Collections.Generic;
using EmployeeTraining.Employee;
using EmployeeTraining.TrainingApp;
using HarmonyLib;
using MyBox;
using UnityEngine;

namespace EmployeeTraining.EmployeeCsHelper;

public class CsHelperAppTab : EmployeeAppTab<CsHelperTrainingProgressItem, CsHelperSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
		ComposeTabButton(baseTabBtnObj, taskbarBtnsObj, "Customer Helper Tab Button", UIHelper.IconCsHelper, "Customer Helpers");
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
		ComposeStatusPanel(
			UIHelper.IconCsHelper,
			"Customer Helper Name",
			bgColorPanel: UIHelper.CsHelperPanel,
			bgColorIntr: UIHelper.CsHelperZone,
			bgColorExp: UIHelper.CsHelperExp,
			bgColorRmSlider: UIHelper.CsHelperExp,
			bgColorRmSeal: UIHelper.CsHelperSeal,
			detailCellSize: new Vector2(95f, 25f),
			detailParamCount: 3,
			detailParams: new DetailParam[3]
			{
				new DetailParam
				{
					Name = "SPM",
					LabelKey = "Scans Per Minute",
					ValueKey = "SPM Range",
					ValueArgs = new string[2] { "<MIN>", "<MAX>" }
				},
				new DetailParam
				{
					Name = "Rapidity",
					LabelKey = "Rapidity",
					ValueKey = "Speed",
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
		ComposeTabScreen("Customer Helpers Tab", "cshelpers", UIHelper.TabCsHelperBg);
		ComposeNoEmployee("No Customer Helpers", "NO CUSTOMER HELPERS HIRED", new Color(0.48f, 0.64f, 0.86f, 1f));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Customer Helper Tab Button", "cshelpers");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Customer Helper Status Panel", (CsHelperSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		List<int> value = Traverse.Create((object)Singleton<EmployeeManager>.Instance).Field<List<int>>("m_CustomerHelpersData").Value;
		DeleteEmployee((CsHelperSkill)skill, value);
	}
}
