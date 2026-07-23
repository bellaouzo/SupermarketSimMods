# Employee Training Program

BepInEx 6 (IL2CPP) port of Tsuteto’s Employee Training Program for Supermarket Simulator v1.4.2.

## Build

From repo root:

```powershell
dotnet build "mods\EmployeeTrainingProgram\EmployeeTrainingProgram.csproj" -c Release
```

Output: `mods/EmployeeTrainingProgram/bin/EmployeeTrainingProgram.dll`  
(also copied to the game `BepInEx\plugins` folder when `GameDir` in `Directory.Build.props` is valid).

## Vanilla click-boost

Training level stretches the in-game click boost duration (medium curve: **1× at lvl 1 → 2× at lvl 100**), on top of the stacked speed/work boosts.

## Training save data

Levels are stored as JSON (not Easy Save) under:

`%userprofile%\AppData\LocalLow\Nokta Games\Supermarket Simulator\EmployeeTraining\`

After saving in-game you should see files like `EmployeeTraining-slot_0.json`. Better Save System is not required.

## Co-op

In multiplayer the **host** owns training XP/levels and publishes them via Photon room properties (`etp_v1`). Guests apply the host blob and should see matching gauges/speeds. Only the host can Train / unlock grades. Everyone needs the same mod version.

## Docs

See repo root [`AGENTS.md`](../../AGENTS.md) for architecture, XP hooks, and 1.4.2 pitfalls.
