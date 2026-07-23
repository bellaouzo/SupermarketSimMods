using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace MultiBoxCarry;

internal static class PluginConfig
{
	internal static ConfigEntry<bool> ShowHud;
	internal static ConfigEntry<bool> CycleWithScroll;
	internal static ConfigEntry<bool> InvertScroll;
	internal static ConfigEntry<Key> CycleNextKey;
	internal static ConfigEntry<Key> CyclePrevKey;
	internal static ConfigEntry<bool> SelectThenConfirm;
	internal static ConfigEntry<Key> ConfirmSwitchKey;

	internal static void Bind(ConfigFile config)
	{
		ShowHud = config.Bind(
			"HUD",
			"ShowHeldBoxesList",
			true,
			"Show a simple list of boxes you are currently holding.");

		CycleWithScroll = config.Bind(
			"Controls",
			"CycleWithScroll",
			true,
			"Use mouse scroll wheel to cycle the held box (or move the HUD selection in SelectThenConfirm mode).");

		InvertScroll = config.Bind(
			"Controls",
			"InvertScroll",
			false,
			"Invert scroll wheel direction.");

		SelectThenConfirm = config.Bind(
			"Controls",
			"SelectThenConfirm",
			false,
			"If true, scroll only moves the HUD highlight; press ConfirmSwitchKey to switch to that box. If false, scroll switches immediately.");

		ConfirmSwitchKey = config.Bind(
			"Controls",
			"ConfirmSwitchKey",
			Key.R,
			"Key used to switch to the highlighted box when SelectThenConfirm is enabled.");

		CycleNextKey = config.Bind(
			"Controls",
			"CycleNextKey",
			Key.RightBracket,
			"Key to cycle to the next queued box. Set to None to disable.");

		CyclePrevKey = config.Bind(
			"Controls",
			"CyclePrevKey",
			Key.LeftBracket,
			"Key to cycle to the previous queued box. Set to None to disable.");
	}
}
