using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmployeeTraining.Employee;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;
using EmployeeTraining.Localization;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using __Project__.Scripts.Computer;
using __Project__.Scripts.ControllerInputModule;
using __Project__.Scripts.ControllerInputModule.EventHandlers;
using __Project__.Scripts.UI;

using EmployeeTraining;
namespace EmployeeTraining.TrainingApp;

public class PCTrainingApp : MonoBehaviour
{
	public PCTrainingApp(IntPtr ptr) : base(ptr) { }

	public static PCTrainingApp Instance { get; private set; }

	private AppWindow baseApp;

	private GameObject unlockApprWindowObj;

	private GameObject panelTmpl;

	private CashierAppTab cashierTab;

	private IEmployeeAppTab[] employeeTabRegistry;

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void Awake()
	{
		Instance = this;
		Plugin.LogDebug("Initializing Training App");
		cashierTab = new CashierAppTab();
		employeeTabRegistry = new IEmployeeAppTab[7]
		{
			cashierTab,
			new RestockerAppTab(),
			new CsHelperAppTab(),
			new SecurityAppTab(),
			new JanitorAppTab(),
			new BakerAppTab(),
			new IceCreamHelperAppTab()
		};
		GameObject screenObject = GameObject.Find("---GAME---/Computer &&/Screen");
		GameObject shortcuts = GameObject.Find("---GAME---/Computer &&/Screen/Desktop Canvas/App Shortcuts");
		GameObject managementApp = GameObject.Find("---GAME---/Computer &&/Screen/Management App");
		GameObject managementExe = GameObject.Find("---GAME---/Computer &&/Screen/Desktop Canvas/App Shortcuts/Management.Exe");
		if (screenObject == null || shortcuts == null || managementApp == null || managementExe == null)
		{
			Plugin.LogError($"Training App UI roots missing. screen={screenObject != null}, shortcuts={shortcuts != null}, managementApp={managementApp != null}, managementExe={managementExe != null}");
			return;
		}
		try
		{
			LoadScreen(screenObject, managementApp);
			Plugin.LogInfo("Loaded Training App window");
			GameObject trainingExe = Object.Instantiate(managementExe, shortcuts.transform, true);
			trainingExe.name = "Training.Exe";
			GridLayoutGroup component = shortcuts.GetComponent<GridLayoutGroup>();
			if (component != null)
			{
				component.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				component.constraintCount = 5;
				component.cellSize = new Vector2(60f, 40f);
			}
			Transform nameTf = trainingExe.transform.Find("Name");
			if (nameTf != null)
			{
				GameObject nameObj = nameTf.gameObject;
				LocalizeStringEvent localize = nameObj.GetComponent<LocalizeStringEvent>();
				if (localize != null)
				{
					Object.Destroy(localize);
				}
				nameObj.AddComponent<StringLocalizeTranslator>().Key = "TRAINING";
			}
			ButtonHandler buttonHandler = trainingExe.GetComponent<ButtonHandler>();
			MouseClickSFX clickSfx = trainingExe.GetComponent<MouseClickSFX>();
			if (buttonHandler != null)
			{
				UnityEvent onClick = new UnityEvent();
				onClick.AddListener((UnityAction)(System.Action)OpenApp);
				if (clickSfx != null)
				{
					onClick.AddListener((UnityAction)(System.Action)clickSfx.Click);
				}
				buttonHandler.m_OnClick = onClick;
			}
			LoadAppToComputerOS();
			Plugin.LogInfo("Training App registered with Computer OS");
			SyncExistingEmployees();
		}
		catch (Exception ex)
		{
			Plugin.LogError("Failed to initialize Training App: " + ex);
		}
	}

	private static void SyncExistingEmployees()
	{
		try
		{
			CashierSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Cashier sync failed: " + ex.Message);
		}
		try
		{
			RestockerSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Restocker sync failed: " + ex.Message);
		}
		try
		{
			BakerSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Baker sync failed: " + ex.Message);
		}
		try
		{
			IceCreamHelperSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("IceCreamHelper sync failed: " + ex.Message);
		}
		try
		{
			JanitorSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Janitor sync failed: " + ex.Message);
		}
		try
		{
			SecuritySkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("Security sync failed: " + ex.Message);
		}
		try
		{
			CsHelperSkillManager.Instance.SyncExisting();
		}
		catch (Exception ex)
		{
			Plugin.LogWarn("CsHelper sync failed: " + ex.Message);
		}
	}

	private void LoadScreen(GameObject screenObject, GameObject managementApp)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = Object.Instantiate<GameObject>(managementApp, screenObject.transform, true);
		((Object)val).name = "Training App";
		val.SetActive(false);
		baseApp = val.GetComponent<AppWindow>();
		((BaseWindow)baseApp).WindowName = "Training";
		unlockApprWindowObj = ComposeUnlockApprovalWindow(val);
		GameObject gameObject = ((Component)val.transform.Find("App Title")).gameObject;
		ComposeAppTitle(gameObject);
		GameObject gameObject2 = ((Component)val.transform.Find("Taskbar")).gameObject;
		gameObject2.transform.SetParent(val.transform);
		ComposeTaskbar(gameObject2, managementApp);
		GameObject gameObject3 = ((Component)val.transform.Find("Tabs")).gameObject;
		ComposeTabs(gameObject3, gameObject2, managementApp);
		val.transform.position = managementApp.transform.position;
		val.transform.rotation = managementApp.transform.rotation;
		Vector3 localScale = managementApp.transform.localScale;
		val.transform.localScale = new Vector3(localScale.x, localScale.y, localScale.z);
		Object.Destroy((Object)(object)val.GetComponent<GamePadUIPanel>());
		Object.Destroy((Object)(object)val.GetComponent<GamepadUIFunctionLibrary>());
		Object.Destroy((Object)(object)val.GetComponent<GamepadUIConfirm>());
		Object.Destroy((Object)(object)val.GetComponent<GamepadUIBack>());
		Object.Destroy((Object)(object)val.GetComponent<GamepadUIShoulder>());
		Object.Destroy((Object)(object)val.GetComponent<GamepadUITrigger>());
	}

	private void ComposeAppTitle(GameObject baseObj)
	{
		Transform textTf = baseObj.transform.Find("Text (TMP)")
			?? baseObj.transform.Find("Text")
			?? baseObj.transform.Find("Title");
		if (textTf == null)
		{
			TextMeshProUGUI tmp = baseObj.GetComponentInChildren<TextMeshProUGUI>(true);
			if (tmp != null)
			{
				textTf = tmp.transform;
			}
		}
		if (textTf != null)
		{
			GameObject textObj = textTf.gameObject;
			LocalizeStringEvent localize = textObj.GetComponent<LocalizeStringEvent>();
			if (localize != null)
			{
				Object.Destroy(localize);
			}
			StringLocalizeTranslator translator = textObj.GetComponent<StringLocalizeTranslator>() ?? textObj.AddComponent<StringLocalizeTranslator>();
			translator.Key = "TRAINING";
		}
		else
		{
			Plugin.LogWarn("Training App title text not found; continuing without rename");
		}
		Transform exitTf = baseObj.transform.Find("Exit");
		if (exitTf == null)
		{
			Plugin.LogWarn("Training App Exit button not found");
			return;
		}
		ButtonHandler buttonHandler = exitTf.GetComponent<ButtonHandler>();
		MouseClickSFX mouseClickSfx = exitTf.GetComponent<MouseClickSFX>();
		if (buttonHandler == null)
		{
			return;
		}
		UnityEvent onClick = new UnityEvent();
		onClick.AddListener((UnityAction)(System.Action)(() =>
		{
			CloseApp();
			mouseClickSfx?.Click();
		}));
		buttonHandler.m_OnClick = onClick;
	}

	private void ComposeTaskbar(GameObject taskbarObj, GameObject managementApp)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		GridLayoutGroup componentInChildren = taskbarObj.GetComponentInChildren<GridLayoutGroup>();
		componentInChildren.cellSize = new Vector2(88f, 22f);
		componentInChildren.spacing = new Vector2(4f, 0f);
		GameObject gameObject = ((Component)taskbarObj.transform.Find("Buttons")).gameObject;
		for (int i = 0; i < gameObject.transform.childCount; i++)
		{
			Object.Destroy((Object)(object)((Component)gameObject.transform.GetChild(i)).gameObject);
		}
		Plugin.LogDebug(cashierTab.GetType());
		cashierTab.ComposeTabButton(managementApp, gameObject, out var tabBtnObj);
		IEmployeeAppTab[] array = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab in array)
		{
			employeeAppTab.ComposeTabButton(tabBtnObj, gameObject);
		}
		Object.Destroy((Object)(object)((Component)taskbarObj.transform.Find("GPIcon (1)")).gameObject);
		Object.Destroy((Object)(object)((Component)taskbarObj.transform.Find("GPIcon (2)")).gameObject);
	}

	private void ComposeTabs(GameObject tabsObj, GameObject taskbarObj, GameObject managementApp)
	{
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Expected O, but got Unknown
		for (int i = 0; i < tabsObj.transform.childCount; i++)
		{
			GameObject gameObject = ((Component)tabsObj.transform.GetChild(i)).gameObject;
			if (((Object)gameObject).name != "Licenses Tab")
			{
				Object.Destroy((Object)(object)gameObject);
			}
		}
		GameObject gameObject2 = ((Component)tabsObj.transform.Find("Licenses Tab")).gameObject;
		((Object)gameObject2).name = "Cashiers Tab";
		gameObject2.SetActive(true);
		Object.Destroy((Object)(object)((Component)gameObject2.transform.Find("Purchased License Indicator")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject2.transform.Find("Unlocked All Licenses")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject2.transform).GetComponent<LicensesTab>());
		Object.Destroy((Object)(object)((Component)gameObject2.transform).GetComponent<GamepadSelectableParent>());
		Transform val = gameObject2.transform.Find("Licenses Scroll View/Viewport/Content");
		for (int j = 0; j < val.childCount; j++)
		{
			Object.Destroy((Object)(object)((Component)val.GetChild(j)).gameObject);
		}
		Transform val2 = gameObject2.transform.Find("Licenses Scroll View");
		((Object)val2).name = "Scroll View";
		Image component = ((Component)val2).GetComponent<Image>();
		((Graphic)component).color = new Color(0.9f, 0.93f, 0.88f, 1f);
		WindowTab component2 = gameObject2.GetComponent<WindowTab>();
		component2.TabName = "cashiers";
		Transform val3 = gameObject2.transform.Find("Scroll View/Viewport/Content");
		((Object)val3).name = "Status";
		UIHelper.PinStatusList(val3);
		Transform val4 = gameObject2.transform.Find("Scroll View");
		Image component4 = ((Component)val4).GetComponent<Image>();
		((Graphic)component4).color = new Color(0.86f, 0.92f, 0.96f, 1f);
		IEmployeeAppTab[] array = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab in array)
		{
			employeeAppTab.CreateTabScreen(gameObject2, tabsObj);
		}
		IEmployeeAppTab[] array2 = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab2 in array2)
		{
			employeeAppTab2.ComposeTabScreen();
		}
		GameObject gameObject3 = ((Component)managementApp.transform.Find("Tabs/Hiring Tab/Scroll View/Viewport/Content/Cashiers/Image/Scroll View/Viewport/Content/Cashier Item")).gameObject;
		panelTmpl = new GameObject("Cashier Status Panel Templates");
		GameObject val5 = Object.Instantiate<GameObject>(gameObject3, panelTmpl.transform, false);
		ComposeBasePanel(val5);
		IEmployeeAppTab[] array3 = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab3 in array3)
		{
			employeeAppTab3.CreateStatusPanel(val5, panelTmpl);
		}
		IEmployeeAppTab[] array4 = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab4 in array4)
		{
			employeeAppTab4.ComposeStatusPanel();
		}
		TabManager component5 = tabsObj.GetComponent<TabManager>();
		IEmployeeAppTab[] array5 = employeeTabRegistry;
		foreach (IEmployeeAppTab employeeAppTab5 in array5)
		{
			employeeAppTab5.AddTabEvent(taskbarObj, component5);
		}
	}

	[HideFromIl2Cpp]
	public void RegisterEmployee(IEmployeeSkill skill)
	{
		employeeTabRegistry.First((IEmployeeAppTab ui) => ui.Matches(skill)).RegisterEmployee(skill, panelTmpl, unlockApprWindowObj);
	}

	[HideFromIl2Cpp]
	public void DeleteEmployee(IEmployeeSkill skill)
	{
		employeeTabRegistry.First((IEmployeeAppTab ui) => ui.Matches(skill)).DeleteEmployee(skill);
	}

	private void ComposeBasePanel(GameObject panelObj)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Expected O, but got Unknown
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Expected O, but got Unknown
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Expected O, but got Unknown
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03df: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Expected O, but got Unknown
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0472: Unknown result type (might be due to invalid IL or missing references)
		//IL_0486: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Expected O, but got Unknown
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_058c: Expected O, but got Unknown
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_063f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0656: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Expected O, but got Unknown
		//IL_06ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_06dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0769: Unknown result type (might be due to invalid IL or missing references)
		//IL_0780: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08da: Unknown result type (might be due to invalid IL or missing references)
		//IL_08eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0941: Unknown result type (might be due to invalid IL or missing references)
		//IL_0948: Expected O, but got Unknown
		//IL_096d: Unknown result type (might be due to invalid IL or missing references)
		//IL_097c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c6: Expected O, but got Unknown
		//IL_09eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a11: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a41: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a50: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a96: Expected O, but got Unknown
		//IL_0abb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b31: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b38: Expected O, but got Unknown
		//IL_0b5d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9d: Expected O, but got Unknown
		//IL_0bc2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c01: Expected O, but got Unknown
		//IL_0c26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c96: Expected O, but got Unknown
		//IL_0cbb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d15: Expected O, but got Unknown
		//IL_0d3a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0da6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df8: Expected O, but got Unknown
		//IL_0e1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e68: Expected O, but got Unknown
		//IL_0e8d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eb6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f3e: Expected O, but got Unknown
		//IL_0f63: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f72: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fec: Unknown result type (might be due to invalid IL or missing references)
		//IL_1005: Unknown result type (might be due to invalid IL or missing references)
		//IL_100c: Expected O, but got Unknown
		//IL_1031: Unknown result type (might be due to invalid IL or missing references)
		//IL_107f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1086: Expected O, but got Unknown
		//IL_10ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_10b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_10dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_10fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1102: Unknown result type (might be due to invalid IL or missing references)
		//IL_1132: Unknown result type (might be due to invalid IL or missing references)
		//IL_1139: Expected O, but got Unknown
		//IL_115e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1163: Unknown result type (might be due to invalid IL or missing references)
		//IL_117c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1181: Unknown result type (might be due to invalid IL or missing references)
		//IL_1195: Unknown result type (might be due to invalid IL or missing references)
		RectTransform component = panelObj.GetComponent<RectTransform>();
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = new Vector2(0f, 0f);
		panelObj.transform.localScale = new Vector3(1f, 1f, 1f);
		panelObj.transform.localPosition = new Vector3(0f, 0f, 0f);
		Transform val = panelObj.transform.Find("Elements");
		val.localPosition = new Vector3(80f, -57.5f, 0f);
		Plugin.LogDebug("Preparing interaction zone of base panel");
		const float zoneW = 158f;
		const float zoneH = 22f;
		const float zoneMidY = -11f;
		const float btnW = 86f;
		const float btnH = 15f;
		const float priceW = 54f;
		GameObject val2 = Il2CppHelpers.NewGameObject("Interaction Zone", typeof(RectTransform), typeof(Image));
		val2.transform.SetParent(panelObj.transform);
		val2.SetupObject(new Vector3(235f, -106f, 0f), (Vector2?)new Vector2(zoneW, zoneH), (Vector2?)new Vector2(1f, 1f));
		Image component2 = val2.GetComponent<Image>();
		component2.sprite = Utils.FindResourceByName<Sprite>("button_corner_square2_23");
		component2.type = (Image.Type)1;
		component2.pixelsPerUnitMultiplier = 2.64f;
		((Graphic)component2).color = UIHelper.InteractionZone;
		GameObject priceChip = Il2CppHelpers.NewGameObject("Price Chip", typeof(RectTransform), typeof(Image));
		priceChip.transform.SetParent(val2.transform);
		float priceCenterX = -zoneW + 6f + priceW * 0.5f;
		priceChip.SetupObject(new Vector3(priceCenterX, zoneMidY, 0f), (Vector2?)new Vector2(priceW, 15f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image priceChipImage = priceChip.GetComponent<Image>();
		priceChipImage.sprite = Utils.FindResourceByName<Sprite>("button_corner_rectangle3_1");
		priceChipImage.type = (Image.Type)1;
		priceChipImage.pixelsPerUnitMultiplier = 20f;
		((Graphic)priceChipImage).color = UIHelper.PriceChip;
		GameObject val3 = Il2CppHelpers.NewGameObject("Training Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MouseClickSFX), typeof(Outline));
		val3.transform.SetParent(val2.transform);
		val3.SetupObject(new Vector3(-4f, zoneMidY, 0f), (Vector2?)new Vector2(btnW, btnH), (Vector2?)new Vector2(1f, 0.5f));
		Image component3 = val3.GetComponent<Image>();
		component3.sprite = Utils.FindResourceByName<Sprite>("button_corner_rectangle3_1");
		component3.type = (Image.Type)1;
		component3.pixelsPerUnitMultiplier = 20f;
		Button component4 = val3.GetComponent<Button>();
		((Selectable)component4).image = component3;
		UIHelper.StyleActionButton(component3, component4, UIHelper.TrainButton);
		GameObject val4 = Il2CppHelpers.NewGameObject("Text", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val4.transform.SetParent(val3.transform);
		Vector3 pos = new Vector3(-btnW * 0.5f, 0f, 0f);
		Vector2 size = new Vector2(btnW - 6f, btnH - 1f);
		Vector2? pivot = new Vector2(0.5f, 0.5f);
		val4.SetupText(pos, size, 8.5f, (HorizontalAlignmentOptions)2, null, pivot, "Train to Level Up");
		TextMeshProUGUI component5 = val4.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component5).autoSizeTextContainer = false;
		((TMP_Text)component5).enableAutoSizing = true;
		GameObject val5 = Object.Instantiate<GameObject>(val3, val2.transform);
		((Object)val5).name = "Unlock Button";
		val5.SetActive(false);
		StringLocalizeTranslator componentInChildren = val5.GetComponentInChildren<StringLocalizeTranslator>();
		componentInChildren.Key = "Unlock Higher Grade";
		Image unlockImage = val5.GetComponent<Image>();
		Button unlockButton = val5.GetComponent<Button>();
		UIHelper.StyleActionButton(unlockImage, unlockButton, new Color(0.78f, 0.52f, 0.16f, 1f));
		GameObject val6 = Il2CppHelpers.NewGameObject("Total Price Text", typeof(TextMeshProUGUI));
		val6.transform.SetParent(priceChip.transform);
		Vector2 size2 = new Vector2(priceW - 4f, 14f);
		pivot = new Vector2(0.5f, 0.5f);
		val6.SetupText(new Vector3(0f, -0.5f, 0f), size2, 11f, (HorizontalAlignmentOptions)2, new Color(1f, 1f, 1f, 0.96f), pivot);
		TextMeshProUGUI component6 = val6.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component6).autoSizeTextContainer = false;
		((TMP_Text)component6).enableAutoSizing = false;
		((TMP_Text)component6).verticalAlignment = VerticalAlignmentOptions.Middle;
		((TMP_Text)component6).margin = new Vector4(0f, 1f, 0f, 0f);
		GameObject val7 = Il2CppHelpers.NewGameObject("Head Gauge Toggle", typeof(RectTransform), typeof(Toggle));
		val7.transform.SetParent(panelObj.transform);
		val7.SetupObject(new Vector3(-65f, -117f), (Vector2?)new Vector2(12f, 12f), (Vector2?)new Vector2(0f, 0.5f));
		GameObject val8 = Il2CppHelpers.NewGameObject("Background", typeof(Image));
		val8.transform.SetParent(val7.transform);
		val8.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)new Vector2(12f, 12f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image component7 = val8.GetComponent<Image>();
		component7.sprite = Utils.FindResourceByName<Sprite>("UISprite");
		((Graphic)component7).color = new Color(0.8431f, 0.9333f, 1f, 1f);
		component7.pixelsPerUnitMultiplier = 2f;
		component7.type = (Image.Type)1;
		GameObject val9 = Il2CppHelpers.NewGameObject("Checkmark", typeof(Image), typeof(Outline));
		val9.transform.SetParent(val8.transform);
		val9.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)new Vector2(9f, 9f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image component8 = val9.GetComponent<Image>();
		component8.sprite = Utils.FindResourceByName<Sprite>("icon_check");
		((Graphic)component8).color = new Color(0f, 0f, 0f, 1f);
		Outline component9 = val9.GetComponent<Outline>();
		((Shadow)component9).effectColor = new Color(0f, 0f, 0f, 1f);
		((Shadow)component9).effectDistance = new Vector2(0.2f, 0.2f);
		Toggle component10 = val7.GetComponent<Toggle>();
		((Selectable)component10).image = component7;
		component10.graphic = (Graphic)(object)component8;
		GameObject val10 = Il2CppHelpers.NewGameObject("Head Gauge Option", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val10.transform.SetParent(panelObj.transform);
		Vector3 pos3 = new Vector3(-55f, -117f, 0f);
		Vector2 size3 = new Vector2(140f, 10f);
		pivot = new Vector2(0f, 0.5f);
		val10.SetupText(pos3, size3, 9.5f, (HorizontalAlignmentOptions)1, new Color(0.22f, 0.32f, 0.42f, 0.9f), pivot, "Show head gauge");
		Plugin.LogDebug("Preparing info part of base panel");
		GameObject gameObject = ((Component)panelObj.transform.Find("Elements/Info")).gameObject;
		RectTransform component11 = gameObject.GetComponent<RectTransform>();
		component11.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 310f);
		component11.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 115f);
		component11.anchoredPosition = new Vector2(0f, 0f);
		component11.pivot = new Vector2(0f, 1f);
		gameObject.transform.localPosition = new Vector3(-75f, 52.5f, 0f);
		Plugin.LogDebug("- Sweeping unnecessary objects");
		Object.Destroy((Object)(object)panelObj.GetComponent<CashierItem>());
		Object.Destroy((Object)(object)((Component)panelObj.transform.Find("Hire Button")).gameObject);
		Object.Destroy((Object)(object)((Component)panelObj.transform.Find("Fire Button")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject.transform.Find("Description")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject.transform.Find("Daily Wage")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject.transform.Find("Hiring Cost")).gameObject);
		Object.Destroy((Object)(object)((Component)gameObject.transform.Find("Requirements")).gameObject);
		Object.Destroy((Object)(object)((Component)panelObj.transform.Find("GPIcon")).gameObject);
		Object.Destroy((Object)(object)panelObj.GetComponent<Selectable>());
		Object.Destroy((Object)(object)panelObj.GetComponent<GamepadUISelectable>());
		Object.Destroy((Object)(object)panelObj.GetComponent<HiringDropdownScroller>());
		GameObject gameObject2 = ((Component)gameObject.transform.Find("Employee Name")).gameObject;
		RectTransform component12 = gameObject2.GetComponent<RectTransform>();
		gameObject2.SetupObject(new Vector3(45f, -4f, 0f), pivot: (Vector2?)new Vector2(0f, 1f), size: (Vector2?)new Vector2(135f, 20f));
		Plugin.LogDebug("- Preparing exp part");
		GameObject val11 = Il2CppHelpers.NewGameObject("Exp Label", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val11.transform.SetParent(gameObject.transform);
		val11.SetupText(new Vector3(190f, -8f, 0f), new Vector2(20f, 15f), 12f, (HorizontalAlignmentOptions)1, null, null, "Exp.");
		GameObject val12 = Il2CppHelpers.NewGameObject("Exp Value", typeof(TextMeshProUGUI));
		val12.transform.SetParent(gameObject.transform);
		Vector3 pos4 = new Vector3(295f, -8f, -0f);
		Vector2 size4 = new Vector2(80f, 15f);
		pivot = new Vector2(1f, 1f);
		val12.SetupText(pos4, size4, 12f, (HorizontalAlignmentOptions)4, null, pivot);
		GameObject val13 = ComposeSlider(gameObject, "Exp Slider", new Vector3(242.5f, -25f), new Vector2(105f, 10f), Color.white);
		val13.SetActive(true);
		GameObject val14 = Il2CppHelpers.NewGameObject("Level", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val14.transform.SetParent(gameObject.transform);
		Vector3 pos5 = new Vector3(45f, -25f, -0f);
		Vector2 size5 = new Vector2(135f, 15f);
		string[] args = new string[2] { "<LVL>", "<GRADE>" };
		val14.SetupText(pos5, size5, 12f, (HorizontalAlignmentOptions)1, null, null, "Level", args);
		Plugin.LogDebug("- Preparing skill params");
		GameObject val15 = Il2CppHelpers.NewGameObject("Detail Params", typeof(GridLayoutGroup));
		val15.transform.SetParent(gameObject.transform);
		val15.SetupObject(new Vector3(15f, -32f, 0f), (Vector2?)new Vector2(-15f, 25f), (Vector2?)null);
		Plugin.LogDebug("- Preparing roadmap");
		GameObject val16 = Il2CppHelpers.NewGameObject("Roadmap Title", typeof(RectTransform));
		val16.transform.SetParent(gameObject.transform);
		val16.SetupObject(new Vector3(15f, -70f, 0f));
		GameObject val17 = Il2CppHelpers.NewGameObject("Divider", typeof(RawImage));
		val17.transform.SetParent(val16.transform);
		val17.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)new Vector2(280f, 1f), (Vector2?)null);
		RawImage component13 = val17.GetComponent<RawImage>();
		component13.texture = (Texture)(object)Utils.FindResourceByName<Texture2D>("UnityWhite");
		((Graphic)component13).color = UIHelper.Divider;
		GameObject val18 = Il2CppHelpers.NewGameObject("Text", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val18.transform.SetParent(val16.transform);
		val18.SetupText(new Vector3(0f, -3f, 0f), new Vector2(200f, 10f), 6f, (HorizontalAlignmentOptions)1, null, null, "Mastery Roadmap");
		GameObject val19 = Il2CppHelpers.NewGameObject("Roadmap", typeof(GridLayoutGroup));
		val19.transform.SetParent(gameObject.transform);
		val19.SetupObject(new Vector3(15f, -65f, 0f), (Vector2?)new Vector2(-15f, 35f), (Vector2?)null);
		float num = 54f;
		GridLayoutGroup component14 = val19.GetComponent<GridLayoutGroup>();
		component14.cellSize = new Vector2(num, 35f);
		component14.constraint = (GridLayoutGroup.Constraint)1;
		component14.constraintCount = 5;
		component14.spacing = new Vector2(2f, 0f);
		Vector2 val20 = default(Vector2);
		val20 = new Vector2(num, 12f);
		Grade[] list = Grade.List;
		foreach (Grade grade in list)
		{
			GameObject val21 = Il2CppHelpers.NewGameObject(grade.Name, typeof(RectTransform));
			val21.transform.SetParent(val19.transform);
			val21.SetupObject(new Vector3(0f, 0f, 0f));
			GameObject val22 = Il2CppHelpers.NewGameObject("Label", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
			val22.transform.SetParent(val21.transform);
			UIHelper.SetupText(
				pos: new Vector3(0f, 0f, 0f),
				size: new Vector2(num - 5f, 12f),
				pivot: (Vector2?)new Vector2(0.5f, 1f),
				obj: val22,
				fontsize: 8f,
				align: (HorizontalAlignmentOptions)2,
				color: (Color?)(Color32)(grade.Color),
				key: grade.Name,
				args: null);
			GameObject val23 = ComposeSlider(val21, "Slider", new Vector3(0f, -13f, 0f), val20, (Color32)(grade.Color));
			GameObject val24 = Il2CppHelpers.NewGameObject("Checkmark", typeof(Image), typeof(Outline));
			val24.transform.SetParent(val23.transform);
			val24.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)new Vector2(12f, 12f), (Vector2?)new Vector2(0.5f, 1f));
			Image component15 = val24.GetComponent<Image>();
			component15.sprite = Utils.FindResourceByName<Sprite>("icon_check");
			Outline component16 = val24.GetComponent<Outline>();
			((Shadow)component16).effectColor = (Color32)(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			((Shadow)component16).effectDistance = new Vector2(0.2f, 0.2f);
			val24.SetActive(false);
			GameObject val25 = Il2CppHelpers.NewGameObject("Seal", typeof(RectTransform));
			val25.transform.SetParent(val21.transform);
			val25.SetupObject(new Vector3(0f, -13f, 0f));
			val25.SetActive(false);
			GameObject val26 = Il2CppHelpers.NewGameObject("Fill", typeof(RectTransform), typeof(Image));
			val26.transform.SetParent(val25.transform);
			val26.SetupObject(new Vector3(0f, 0f, 0f), val20);
			RectTransform component17 = val26.GetComponent<RectTransform>();
			component17.pivot = new Vector2(0.5f, 1f);
			Image component18 = val26.GetComponent<Image>();
			((Graphic)component18).color = (Color32)(new Color32((byte)34, (byte)74, (byte)96, byte.MaxValue));
			GameObject val27 = Il2CppHelpers.NewGameObject("Text", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
			val27.transform.SetParent(val25.transform);
			val27.SetupText(
				new Vector3(0f, 0f, 0f),
				val20,
				8f,
				(HorizontalAlignmentOptions)2,
				(Color?)(Color32)(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)32)),
				(Vector2?)new Vector2(0.5f, 1f),
				"Locked",
				null);
		}
	}

	private GameObject ComposeUnlockApprovalWindow(GameObject appObj)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Expected O, but got Unknown
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Expected O, but got Unknown
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Expected O, but got Unknown
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_052b: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
		//IL_057d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Expected O, but got Unknown
		//IL_05d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Expected O, but got Unknown
		//IL_05f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fd: Expected O, but got Unknown
		Sprite sprite = Utils.FindResourceByName<Sprite>("Frame_SpeechBubble01");
		GameObject windowObj = new GameObject("Unlock Approval Window");
		windowObj.transform.SetParent(appObj.transform);
		GameObject obj = windowObj;
		Vector3 pos = new Vector3(0f, 28f, 0f);
		Vector2? pivot = new Vector2(0.5f, 0.5f);
		obj.SetupObject(pos, null, pivot);
		windowObj.SetActive(false);
		GameObject val = Il2CppHelpers.NewGameObject("Backdrop", typeof(RawImage));
		val.transform.SetParent(windowObj.transform);
		val.SetupObject(new Vector3(0f, -40f, 0f), (Vector2?)new Vector2(700f, 460f), (Vector2?)new Vector2(0.5f, 0.5f));
		RawImage component = val.GetComponent<RawImage>();
		((Graphic)component).color = new Color(0.0254f, 0.0354f, 0.0566f, 0.9804f);
		GameObject val2 = Il2CppHelpers.NewGameObject("Window BG", typeof(Image));
		val2.transform.SetParent(windowObj.transform);
		val2.SetupObject(new Vector3(0f, 12.8f, 0f), (Vector2?)new Vector2(350f, 110f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image component2 = val2.GetComponent<Image>();
		component2.sprite = sprite;
		component2.pixelsPerUnitMultiplier = 4f;
		((Graphic)component2).color = new Color(0.9104f, 0.9654f, 1f, 1f);
		component2.type = (Image.Type)1;
		GameObject val3 = Il2CppHelpers.NewGameObject("Message", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val3.transform.SetParent(val2.transform);
		UIHelper.SetupText(pos: new Vector3(0f, 45f, 0f), size: new Vector2(330f, 100f), pivot: (Vector2?)new Vector2(0.5f, 1f), obj: val3, fontsize: 12f, align: (HorizontalAlignmentOptions)1, color: (Color?)new Color(0f, 0f, 0f, 1f), key: "Upgrade warning", args: new string[3] { "<NO>", "<GRADE_NAME>", "<WAGE>" });
		TextMeshProUGUI component3 = val3.GetComponent<TextMeshProUGUI>();
		((TMP_Text)component3).autoSizeTextContainer = false;
		((TMP_Text)component3).enableAutoSizing = true;
		((TMP_Text)component3).verticalAlignment = (VerticalAlignmentOptions)256;
		((TMP_Text)component3).horizontalAlignment = (HorizontalAlignmentOptions)2;
		GameObject val4 = Il2CppHelpers.NewGameObject("Approve Button", typeof(Button), typeof(Image), typeof(Outline), typeof(MouseClickSFX));
		val4.transform.SetParent(val2.transform);
		val4.SetupObject(new Vector3(-60f, -55f, 0f), (Vector2?)new Vector2(80f, 30f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image component4 = val4.GetComponent<Image>();
		component4.sprite = sprite;
		component4.pixelsPerUnitMultiplier = 4f;
		((Graphic)component4).color = new Color(0.1033f, 0.8113f, 0.2143f, 1f);
		component4.type = (Image.Type)1;
		Outline component5 = val4.GetComponent<Outline>();
		((Shadow)component5).effectColor = new Color(0f, 0f, 0f, 1f);
		((Shadow)component5).effectDistance = new Vector2(0.5f, 0.5f);
		Button component6 = val4.GetComponent<Button>();
		((Selectable)component6).image = component4;
		((Selectable)component6).colors = UIHelper.COLOR_BLOCK;
		GameObject val5 = Il2CppHelpers.NewGameObject("Text", typeof(TextMeshProUGUI), typeof(StringLocalizeTranslator));
		val5.transform.SetParent(val4.transform);
		UIHelper.SetupText(pos: new Vector3(0f, 0f, 0f), size: new Vector2(80f, 30f), pivot: (Vector2?)new Vector2(0.5f, 0.5f), obj: val5, fontsize: 15f, align: (HorizontalAlignmentOptions)2, color: (Color?)new Color(1f, 1f, 1f, 1f), key: "Approve", args: (string[])null);
		RectTransform component7 = val5.GetComponent<RectTransform>();
		component7.anchoredPosition = new Vector2(0f, 2f);
		GameObject val6 = Object.Instantiate<GameObject>(val4, val2.transform);
		((Object)val6).name = "Cancel Button";
		val6.SetupObject(new Vector3(60f, -55f, 0f), (Vector2?)new Vector2(80f, 30f), (Vector2?)new Vector2(0.5f, 0.5f));
		Image component8 = val6.GetComponent<Image>();
		((Graphic)component8).color = new Color(0.8118f, 0.2098f, 0.102f, 1f);
		StringLocalizeTranslator componentInChildren = val6.GetComponentInChildren<StringLocalizeTranslator>();
		componentInChildren.Key = "Defer";
		Button component9 = ((Component)windowObj.transform.Find("Window BG/Cancel Button")).GetComponent<Button>();
		component9.onClick = new Button.ButtonClickedEvent();
		((UnityEvent)component9.onClick).AddListener((UnityAction)delegate
		{
			windowObj.SetActive(false);
		});
		((UnityEvent)component9.onClick).AddListener(((UnityAction)(System.Action)(val6.GetComponent<MouseClickSFX>().Click)));
		return windowObj;
	}

	private GameObject ComposeSlider(GameObject gradeObj, string name, Vector3 pos, Vector2 sliderSize, Color color)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Expected O, but got Unknown
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 pivot = default(Vector2);
		pivot = new Vector2(0.5f, 1f);
		GameObject val = Il2CppHelpers.NewGameObject(name, typeof(Slider));
		val.transform.SetParent(gradeObj.transform);
		val.SetupObject(pos, (Vector2?)new Vector2(0f, 0f), (Vector2?)null);
		val.SetActive(false);
		GameObject val2 = Il2CppHelpers.NewGameObject("Background", typeof(RectTransform), typeof(Image));
		val2.transform.SetParent(val.transform);
		val2.SetupObject(new Vector3(0f, 0f, 0f), sliderSize);
		GameObject val3 = Il2CppHelpers.NewGameObject("Fill Area", typeof(RectTransform));
		val3.transform.SetParent(val.transform);
		val3.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)sliderSize, (Vector2?)new Vector2(0.5f, 0.5f));
		GameObject val4 = Il2CppHelpers.NewGameObject("Fill", typeof(CanvasRenderer), typeof(Image));
		val4.transform.SetParent(val3.transform);
		val4.SetupObject(new Vector3(0f, 0f, 0f), (Vector2?)new Vector2(-2f, -2f), (Vector2?)new Vector2(0.5f, 0.5f));
		Slider component = val.GetComponent<Slider>();
		component.fillRect = val4.GetComponent<RectTransform>();
		component.direction = (Slider.Direction)1;
		((Selectable)component).interactable = false;
		((Selectable)component).transition = (Selectable.Transition)0;
		RectTransform component2 = val2.GetComponent<RectTransform>();
		component2.pivot = pivot;
		Image component3 = val2.GetComponent<Image>();
		((Graphic)component3).color = color;
		RectTransform component4 = val3.GetComponent<RectTransform>();
		component4.SetSizeWithCurrentAnchors((RectTransform.Axis)0, sliderSize.x);
		component4.SetSizeWithCurrentAnchors((RectTransform.Axis)1, sliderSize.y);
		component4.pivot = pivot;
		Image component5 = val4.GetComponent<Image>();
		((Graphic)component5).color = (Color32)(new Color32((byte)21, (byte)45, (byte)59, byte.MaxValue));
		return val;
	}

	private static void OpenApp()
	{
		Singleton<ComputerOperatingSystem>.Instance.OpenApp("Training");
	}

	private static void CloseApp()
	{
		Singleton<ComputerOperatingSystem>.Instance.CloseApp("Training");
	}

	public static void LoadAppToComputerOS()
	{
		if (Instance == null || Instance.baseApp == null)
		{
			Plugin.LogWarn("LoadAppToComputerOS skipped: Training App window not ready");
			return;
		}
		ComputerOperatingSystem os = Singleton<ComputerOperatingSystem>.Instance;
		if (os == null)
		{
			Plugin.LogWarn("LoadAppToComputerOS skipped: ComputerOperatingSystem missing");
			return;
		}
		Il2CppReferenceArray<AppWindow> existing = os.m_AppWindows;
		List<AppWindow> windows = new List<AppWindow>();
		if (existing != null)
		{
			foreach (AppWindow window in existing)
			{
				if (window != null && window != Instance.baseApp)
				{
					windows.Add(window);
				}
			}
		}
		windows.Add(Instance.baseApp);
		os.m_AppWindows = new Il2CppReferenceArray<AppWindow>(windows.ToArray());
	}
}
