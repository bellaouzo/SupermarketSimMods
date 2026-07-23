# CS - Shelf Product Swapper (fork)

Fork of CS Shelf Product Swapper for Supermarket Simulator v1.4.2 with **SirW_CustomHints** integration and a co-op hotkey fix.

While the swapper is enabled (**F6**), left-panel hints show for:

- **C** Select Slot / Swap Target
- **X** Cross-Type (On/Off)
- **F6** Swapper (On)

## Build

```powershell
dotnet build "mods\ShelfProductSwapper\ShelfProductSwapper.csproj" -c Release
```

Output: `mods/ShelfProductSwapper/bin/CS-ShelfProductSwapper.dll` (copied to game plugins).

## Smoke test

1. Load a save with labeled shelves and/or box racks.
2. Press **F6** — log should say Swapper ON; hints should appear.
3. Aim at a shelf slot (or price tag/label) — cyan hover marker.
4. Press **C** — yellow selected marker; hint text becomes "Swap Target".
5. Aim at a second compatible slot and press **C** — products/labels swap; selection clears.
6. Press **C** on the same selected slot again — selection clears (cancel).
7. Optional: press **X** to allow cross-type furniture swaps; try shelf ↔ fridge style moves.
8. Box racks: same **C** select/swap flow (cannot mix shelf ↔ rack).
9. Co-op: with 2 players joined, press **F6** / **C** once each — should toggle/select once (not immediately undo).

## Co-op note

Hotkeys run once per frame. Upstream ran on every `FurniturePlacer.Update`, so with 2 placers a single **C** could select then clear in the same frame.

In a Photon room, **any client** can swap. The swapper raises a Photon event with the final slot state and also requests display/rack authority sync so every peer applies the same result.

## Perf / markers

- Aim at **products**, **price tags**, or **labels**. Shelf products are often GPU-instanced (no colliders), so hover also uses camera-ray proximity to shelf anchors.
- Markers sit on the shelf surface, not the stand-point on the floor.
- Native green furniture outlines are **off** by default (`UseNativeOutlines`).
- One non-alloc raycast per frame; marker geometry updates only when hover/selection changes.
