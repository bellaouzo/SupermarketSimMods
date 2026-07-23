using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace ConnectionGuard;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class ConnectionGuardPlugin : BasePlugin
{
	public const string PluginGuid = "ConnectionGuard";
	public const string PluginName = "Connection Guard";
	public const string PluginVersion = "1.0.5";

	internal static ManualLogSource LogSource;

	internal static ConfigEntry<bool> Enabled;
	internal static ConfigEntry<int> DisconnectTimeoutMs;
	internal static ConfigEntry<float> KeepAliveInBackgroundSeconds;
	internal static ConfigEntry<int> SentCountAllowance;
	internal static ConfigEntry<int> QuickResends;
	internal static ConfigEntry<bool> ShowPingHud;
	internal static ConfigEntry<bool> ShowOnlyInMultiplayer;
	internal static ConfigEntry<float> HudOffsetX;
	internal static ConfigEntry<float> HudOffsetY;
	internal static ConfigEntry<bool> DebugLogging;

	private Harmony _harmony;

	public override void Load()
	{
		LogSource = Log;
		Enabled = Config.Bind("General", "Enabled", true, "Master toggle for Connection Guard.");
		DisconnectTimeoutMs = Config.Bind(
			"Timeouts",
			"PhotonDisconnectTimeoutMs",
			45000,
			"Photon peer disconnect timeout in milliseconds. Vanilla is often ~10000. Higher = more tolerant of ping spikes.");
		KeepAliveInBackgroundSeconds = Config.Bind(
			"Timeouts",
			"KeepAliveInBackgroundSeconds",
			120f,
			"How long Photon keeps the connection when the game is in the background.");
		SentCountAllowance = Config.Bind(
			"Timeouts",
			"SentCountAllowance",
			12,
			"Photon resend allowance before giving up on a reliable message. Higher helps bad connections.");
		QuickResends = Config.Bind(
			"Timeouts",
			"QuickResends",
			5,
			"Photon quick-resend attempts for reliable traffic.");
		ShowPingHud = Config.Bind("UI", "ShowPingHud", true, "Show a small ping readout on screen in multiplayer.");
		ShowOnlyInMultiplayer = Config.Bind("UI", "ShowOnlyInMultiplayer", true, "Hide the ping HUD when not in a Photon room.");
		HudOffsetX = Config.Bind("UI", "HudOffsetX", -24f, "Ping HUD X offset from the top-right corner.");
		HudOffsetY = Config.Bind("UI", "HudOffsetY", -120f, "Ping HUD Y offset from the top-right corner (more negative = lower).");
		if (HudOffsetY.Value > -100f)
		{
			HudOffsetY.Value = -120f;
		}

		DebugLogging = Config.Bind("Diagnostics", "DebugLogging", false, "Log when timeout settings are applied.");

		ClassInjector.RegisterTypeInIl2Cpp<ConnectionGuardRuntime>();
		ConnectionGuardRuntime.Create();

		_harmony = new Harmony(PluginGuid);
		_harmony.PatchAll(typeof(ConnectionGuardPlugin).Assembly);
		Log.LogInfo($"{PluginName} {PluginVersion} loaded. Install on every co-op PC for best results.");
	}
}
