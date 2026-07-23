using System;
using BepInEx.Configuration;

namespace EmployeeTraining;

public class Settings
{
	public float GaugeHeight;

	public bool CustomizeLocalization;

	public bool RestockerLog;

	public Settings(ConfigFile cfg)
	{
		ConfigEntry<float> gaugeHeight = cfg.Bind<float>("Gauge Indicator", "Gauge Height", 2.55f, "Height of the XP gauge above employees. Raise further if it overlaps the boost icon.");
		if (Math.Abs(gaugeHeight.Value - 1.8f) < 0.001f)
		{
			gaugeHeight.Value = 2.55f;
		}
		GaugeHeight = gaugeHeight.Value;
		CustomizeLocalization = cfg.Bind<bool>("Localization", "Customize Localization", false, "If true is set, Localization-x.x.x.json file is saved in Supermarket Simulator_Data/CashierTraining folder and you can customize it.").Value;
		RestockerLog = cfg.Bind<bool>("Debug", "Restocker verbose log", false, "Enable restocker verbose log. You also need to set the LogLevels in BepInEx.cfg to \"All\" for output. This may cause fps to drop. Note: This is not for bug reporting but for your own troubleshooting.").Value;
	}
}
