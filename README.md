# SupermarketSimMods

Modified BepInEx plugins for **[Supermarket Simulator](https://store.steampowered.com/app/2670630/Supermarket_Simulator/)** (v1.4.2, IL2CPP), focused on smoother **co-op** and everyday quality-of-life tweaks.

Most of these started as existing community mods. This repo is a maintained fork/port set with multiplayer fixes, sync, and small UX improvements so hosts and guests stay on the same page.

## What’s included

| Mod | Based on / role | What this fork improves |
|-----|-----------------|-------------------------|
| **EmployeeTrainingProgram** | Tsuteto’s Employee Training Program | IL2CPP 1.4.2 port, host-synced levels in co-op, Training.Exe UI polish |
| **DemandSystem** | Demand overlay mods | Day-seeded / list-seeded demand so clients see consistent events |
| **MultiBoxCarry** | Multi-box carry plugins | Per-player inventory, local-only queue, safer networked box occupy |
| **ShelfProductSwapper** | CS Shelf Product Swapper | Frame-guarded input, Photon sync so partners see swaps |
| **SmartStockOrder** | CS Smart Stock Order | Co-op-safe cart adds (`TryAddProduct_Request`), CustomHints |
| **FurnitureAligner** | CS Furniture Aligner | Placement snap/align; Outside bypass disabled in multiplayer rooms |

Optional soft dependency: **[SirW_CustomHints](https://github.com/)** (on-screen key hints). Upstream plugin GUIDs are kept where possible so existing `.cfg` files still apply.

## Requirements

- Supermarket Simulator **v1.4.2** (all DLC OK)
- [BepInEx 6](https://github.com/BepInEx/BepInEx) (IL2CPP / Unity)
- For co-op: **same mod DLLs and configs on every PC**

## Install

1. Install BepInEx 6 for the game and run once so `BepInEx\plugins` exists.
2. Build this repo (below) **or** copy release DLLs into `BepInEx\plugins\`.
3. Launch the game. Check `BepInEx\LogOutput.log` for load errors.

Typical outputs after a Release build:

- `EmployeeTrainingProgram.dll`
- `CS-FurnitureAligner.dll`
- `CS-ShelfProductSwapper.dll`
- `CS-SmartStockOrder.dll`
- (+ DemandSystem / MultiBoxCarry project outputs)

## Build

1. Edit `Directory.Build.props` and set `GameDir` to your game install (needs `BepInEx\interop`).
2. From the repo root:

```powershell
dotnet build SupermarketSimMod.sln -c Release
```

Or build one mod:

```powershell
dotnet build "mods\EmployeeTrainingProgram\EmployeeTrainingProgram.csproj" -c Release
```

When `GameDir` is valid, each project’s build target copies its DLL into `BepInEx\plugins`.

Override the path for a one-off build:

```powershell
dotnet build SupermarketSimMod.sln -c Release -p:GameDir="D:\Games\Supermarket Simulator"
```

## Co-op notes

These forks aim to make multiplayer less desynced than stock single-player-oriented plugins:

- **Host owns** training XP / Train actions; guests receive synced skill data.
- Shelf swaps and smart-order cart adds go through networked game APIs where possible.
- Carry queues and input hotkeys are scoped so one player’s actions don’t steal another’s.
- Use matching versions of every DLL on all machines.

Quick smoke check (2 clients):

1. Demand looks the same for both players  
2. Only you can take boxes from your carry queue  
3. A shelf swap on one PC appears for the other  
4. Training levels match after join / host train  
5. Smart order fills a cart the partner can see  
6. Furniture align still snaps; Outside doesn’t force-valid in MP  

## Credits

- Original authors of Employee Training Program (Tsuteto), CS Furniture Aligner, CS Shelf Product Swapper, CS Smart Stock Order, MultiBoxCarry, Demand, and related community work
- Nokta Games — Supermarket Simulator
- BepInEx / Il2CppInterop tooling

This is an unofficial fan project. Not affiliated with Nokta Games.

## License / use

Check each mod folder and upstream projects for their original licenses. Treat this repo as a personal/community maintenance fork unless a LICENSE file says otherwise.

## Deeper docs

Contributor-oriented notes (hooks, pitfalls, architecture): [`AGENTS.md`](AGENTS.md).
