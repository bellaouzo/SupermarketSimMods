# SupermarketSimMod

Multi-mod workspace for **Supermarket Simulator** v1.4.2 (IL2CPP + BepInEx 6).

## Mods

| Mod | Description |
|-----|-------------|
| **EmployeeTrainingProgram** | Train employees, XP/levels, Training.Exe PC app |
| **DemandSystem** | Day-seeded demand events (co-op friendly) |
| **MultiBoxCarry** | Carry multiple boxes with local/co-op-safe inventory |
| **ShelfProductSwapper** | Swap shelf products; Photon sync in co-op |
| **SmartStockOrder** | Quick stock ordering from tablet/scanner |
| **FurnitureAligner** | Snap/align furniture while placing |

## Build

Set `GameDir` in `Directory.Build.props` (or pass `-p:GameDir=...`), then:

```powershell
dotnet build SupermarketSimMod.sln -c Release
```

Each mod copies its DLL into `BepInEx\plugins` when the game path is valid.

## Docs

See [`AGENTS.md`](AGENTS.md) for architecture notes, co-op checklist, and 1.4.2 pitfalls.
