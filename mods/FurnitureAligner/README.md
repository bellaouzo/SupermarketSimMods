# CS - Furniture Aligner (fork)

Fork of CS Furniture Aligner for Supermarket Simulator v1.4.2 with **SirW_CustomHints** integration.

While placing furniture (and Aligner is enabled), extra left-panel hints show for:

- Edge Align / Center Align / Grid Snap (with On/Off)
- Outside Place
- Reset Nudge
- Aligner toggle

Soft-depends on `SirW_CustomHints`. Without it, alignment still works; hints are skipped.

## Co-op note

Hotkeys are processed once per frame. Older builds toggled once per `FurniturePlacer.Update`, so with 2 players a single U/F8 press could flip ON then OFF immediately.

**Outside Place** bypass is disabled while in a Photon room so host/guest placement rules stay aligned with vanilla network validation. Snap/align still work for the local placer.

## Build

```powershell
dotnet build "mods\FurnitureAligner\FurnitureAligner.csproj" -c Release
```

Output: `mods/FurnitureAligner/bin/CS-FurnitureAligner.dll` (copied to game plugins).

Keep the same plugin GUID so your existing `CS.supermarketsimulator.furniturealigner.cfg` continues to apply.
