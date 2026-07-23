using EmployeeTraining.Employee;
using EmployeeTraining.Localization;
using EmployeeTraining.TrainingApp;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace EmployeeTraining.EmployeeCashier;

public class CashierAppTab : EmployeeAppTab<CashierTrainingProgressItem, CashierSkill>, IEmployeeAppTab
{
	public void ComposeTabButton(GameObject managementApp, GameObject taskbarBtnsObj, out GameObject tabBtnObj)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		GameObject gameObject = ((Component)((Component)managementApp.transform.Find("Taskbar/Buttons")).transform.GetChild(0)).gameObject;
		tabBtnObj = Object.Instantiate<GameObject>(gameObject, taskbarBtnsObj.transform, true);
		((Object)tabBtnObj).name = "Cashiers Tab Button";
		Object.Destroy((Object)(object)tabBtnObj.GetComponentInChildren<LocalizeStringEvent>());
		Sprite sprite = UIHelper.ResolveSprite(UIHelper.IconCashier);
		Transform val = tabBtnObj.transform.Find("Tab Icon");
		((Component)val).GetComponent<Image>().sprite = sprite;
		val.localPosition = new Vector3(-32f, 0f, 0f);
		MyExtensions.GetOrAddComponent<StringLocalizeTranslator>((Component)(object)tabBtnObj.transform.Find("Text (TMP)")).Key = "Cashiers";
		Button component = tabBtnObj.GetComponent<Button>();
		((UnityEventBase)component.onClick).RemoveAllListeners();
		((UnityEventBase)component.onClick).m_PersistentCalls = new PersistentCallGroup();
		MouseClickSFX component2 = tabBtnObj.GetComponent<MouseClickSFX>();
		((UnityEvent)component.onClick).AddListener(((UnityAction)(System.Action)(component2.Click)));
	}

	public void ComposeTabButton(GameObject baseTabBtnObj, GameObject taskbarBtnsObj)
	{
	}

	void IEmployeeAppTab.CreateStatusPanel(GameObject basePanelObj, GameObject panelTmpl)
	{
		base.StatusPanelTmpl = basePanelObj;
	}

	public void ComposeStatusPanel()
	{
		ComposeStatusPanel(
			UIHelper.IconCashier,
			"Cashier Name",
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
					Name = "Payment Time",
					LabelKey = "Payment Time",
					ValueKey = "Payment Time Sec.",
					ValueArgs = new string[1] { "<SEC>" }
				},
				new DetailParamWage()
			},
			bgColorPanel: UIHelper.CashierPanel,
			bgColorIntr: UIHelper.CashierZone,
			bgColorExp: UIHelper.CashierExp,
			bgColorRmSlider: UIHelper.CashierExp,
			bgColorRmSeal: UIHelper.CashierSeal);
	}

	void IEmployeeAppTab.CreateTabScreen(GameObject baseTabObj, GameObject tabsObj)
	{
		base.TabObj = baseTabObj;
	}

	public void ComposeTabScreen()
	{
		base.ListObj = base.TabObj.transform.Find("Scroll View/Viewport/Status");
		Image scrollBg = ((Component)base.TabObj.transform.Find("Scroll View")).GetComponent<Image>();
		((Graphic)scrollBg).color = UIHelper.TabCashierBg;
		ComposeNoEmployee("No Cashiers", "NO CASHIERS HIRED", new Color(0.45f, 0.62f, 0.82f, 1f));
	}

	public void AddTabEvent(GameObject taskbarObj, TabManager tabMgr)
	{
		AddTabBtnEvent(taskbarObj, tabMgr, "Buttons/Cashiers Tab Button", "cashiers");
	}

	public void RegisterEmployee(IEmployeeSkill skill, GameObject panelTmpl, GameObject unlockApprWindowObj)
	{
		RegisterEmployee("Cashier Status Panel", (CashierSkill)skill, unlockApprWindowObj);
	}

	public void DeleteEmployee(IEmployeeSkill skill)
	{
		DeleteEmployee((CashierSkill)skill, Singleton<EmployeeManager>.Instance.CashiersData);
	}
}
