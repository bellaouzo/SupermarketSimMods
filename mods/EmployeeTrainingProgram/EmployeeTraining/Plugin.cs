using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using EmployeeTraining.EmployeeBaker;
using EmployeeTraining.EmployeeCashier;
using EmployeeTraining.EmployeeCsHelper;
using EmployeeTraining.EmployeeIceCream;
using EmployeeTraining.EmployeeJanitor;
using EmployeeTraining.EmployeeRestocker;
using EmployeeTraining.EmployeeSecurity;
using EmployeeTraining.Localization;
using EmployeeTraining.TrainingApp;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using MyBox;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace EmployeeTraining;

[BepInPlugin("jp.tsuteto.sms.EmployeeTrainingProgram", "EmployeeTrainingProgram", "2.6.28")]
[BepInProcess("Supermarket Simulator.exe")]
public class Plugin : BasePlugin
{
	internal class Lifecycle : MonoBehaviour
	{
		public Lifecycle(IntPtr ptr)
			: base(ptr)
		{
		}

		private void Start()
		{
			SceneManager.activeSceneChanged += DelegateSupport.ConvertDelegate<UnityAction<Scene, Scene>>((Delegate)new Action<Scene, Scene>(Plugin.Instance.OnSceneWasLoaded));
			LocalizationSettings.add_SelectedLocaleChanged(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<UnityEngine.Localization.Locale>>((Delegate)new Action<UnityEngine.Localization.Locale>(Plugin.Instance.OnLocaleChanged)));
			UnityEngine.Localization.Locale selectedLocale = LocalizationSettings.SelectedLocale;
			if (selectedLocale != null)
			{
				Localizer.Lang = selectedLocale.m_Identifier.Code;
			}
			SkillIndicatorGenerator.Init();
			CsHelperLogic.Init();
		}

		private void Update()
		{
			ClerkRecoveryPatch.Tick();
			TrainingNetworkSync.Tick();
		}
	}

	public Action GameLoadedEvent;

	public Action GameQuitEvent;

	public Action<string> LocaleChangedEvent;

	public static Plugin Instance { get; private set; }

	public static StringLocalizer Localizer { get; private set; }

	public Settings Settings { get; private set; }

	public ManualLogSource Logger => Log;

	public override void Load()
	{
		Instance = this;
		Settings = new Settings(Config);
		LocalizationSerializer localizationSerializer = new LocalizationSerializer(this);
		Localizer = localizationSerializer.Load();
		GamePatch.plugin = this;

		RegisterIl2CppTypes();

		Log.LogInfo("Applying EmployeeTraining patches");
		Harmony harmony = new Harmony("jp.tsuteto.sms.EmployeeTraining");
		ApplyPatches(harmony);
		ETSaveManager.CreateSaveDirectory();
		AddComponent<Lifecycle>();
		Log.LogInfo("Plugin EmployeeTrainingProgram is loaded!");
	}

	private void ApplyPatches(Harmony harmony)
	{
		foreach (Type type in typeof(Plugin).Assembly.GetTypes())
		{
			try
			{
				harmony.CreateClassProcessor(type).Patch();
			}
			catch (Exception ex)
			{
				Log.LogError($"Failed to apply patches in {type.FullName}: {ex.Message}");
			}
		}
		Log.LogInfo("EmployeeTraining patch pass complete");
	}

	private static void RegisterIl2CppTypes()
	{
		ClassInjector.RegisterTypeInIl2Cpp<Lifecycle>();
		ClassInjector.RegisterTypeInIl2Cpp<StringLocalizeTranslator>();
		ClassInjector.RegisterTypeInIl2Cpp<SkillIndicator>();
		ClassInjector.RegisterTypeInIl2Cpp<ShoppingCustomerList>();
		ClassInjector.RegisterTypeInIl2Cpp<PCTrainingApp>();
		ClassInjector.RegisterTypeInIl2Cpp<CashierTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<CsHelperTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<JanitorTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<RestockerTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<SecurityTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<BakerTrainingProgressItem>();
		ClassInjector.RegisterTypeInIl2Cpp<IceCreamHelperTrainingProgressItem>();
	}

	public void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
	{
		if (newLocale == null)
		{
			return;
		}
		string code = newLocale.m_Identifier.Code;
		Localizer.Lang = code;
		LocaleChangedEvent?.Invoke(code);
	}

	public void OnSceneWasLoaded(Scene activeScene, Scene nextScene)
	{
		string nextName = nextScene.name;
		if (nextName == "Main Menu")
		{
			try
			{
				ETSaveManager.SaveCurrent(force: true);
			}
			catch (Exception ex)
			{
				Log.LogWarning("Training autosave on menu failed: " + ex.Message);
			}
			GameQuitEvent?.Invoke();
		}
		if (nextName == "Main Scene" || nextName == "Multiplayer")
		{
			try
			{
				SaveManager saveManager = Singleton<SaveManager>.Instance;
				if (saveManager != null)
				{
					ETSaveManager.Load(saveManager.CurrentSaveFilePath);
				}
			}
			catch (Exception ex)
			{
				Log.LogWarning("Training reload on scene failed: " + ex.Message);
			}
			GameObject managers = GameObject.Find("---MANAGERS---");
			if (managers == null)
			{
				Log.LogWarning("Could not find ---MANAGERS---; training objects not spawned.");
				return;
			}
			GameObject shoppingList = new GameObject("Shopping Customer List");
			shoppingList.AddComponent<ShoppingCustomerList>();
			shoppingList.transform.SetParent(managers.transform);
			GameObject trainingApp = new GameObject("Training App");
			trainingApp.AddComponent<PCTrainingApp>();
			trainingApp.transform.SetParent(managers.transform);
			GameLoadedEvent?.Invoke();
			EmployeeTraining.EmployeeRestocker.ClerkRecoveryPatch.OnSceneReady();
		}
	}

	public static void LogDebug(object data)
	{
		Instance?.Log.LogDebug(data);
	}

	public static void LogInfo(object data)
	{
		Instance?.Log.LogInfo(data);
	}

	public static void LogWarn(object data)
	{
		Instance?.Log.LogWarning(data);
	}

	public static void LogError(object data)
	{
		Instance?.Log.LogError(data);
	}
}
