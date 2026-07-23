# Connection Guard

BepInEx 6 (IL2CPP) mod for Supermarket Simulator v1.4.2.

## What it does

- Shows a small **PING** readout (top-right) while in multiplayer
- Raises Photon / game disconnect timeouts so brief ping spikes are less likely to kick someone
- Increases reliable-message resend allowance

**Install on every co-op PC** (host and guests). Timeouts are applied per client.

## Config (`BepInEx/config/ConnectionGuard.cfg`)

| Key | Default | Notes |
|-----|---------|--------|
| `PhotonDisconnectTimeoutMs` | `45000` | Vanilla Photon is often ~10000 ms |
| `KeepAliveInBackgroundSeconds` | `120` | Alt-tab grace |
| `SentCountAllowance` | `12` | Reliable resends |
| `QuickResends` | `5` | Fast retry count |
| `ShowPingHud` | `true` | On-screen ping |
| `HudOffsetY` | `-120` | Lower = further below the money/time HUD |

## Build

```powershell
dotnet build "mods\ConnectionGuard\ConnectionGuard.csproj" -c Release
```

Copies `ConnectionGuard.dll` into `BepInEx\plugins` when `GameDir` is set.
