# CS - Smart Stock Order (fork)

Fork of CS Smart Stock Order for Supermarket Simulator v1.4.2 with **SirW_CustomHints** integration.

While the tablet or handheld scanner UI is open, left-panel hints show:

- **V** Fill Empty Racks
- **B** 1 Box If Empty

Market App buttons are added into the category tab row (after Vehicles) via the layout group so they no longer overlay Tools/Vehicles.

## Build

```powershell
dotnet build "mods\SmartStockOrder\SmartStockOrder.csproj" -c Release
```

Output: `mods/SmartStockOrder/bin/CS-SmartStockOrder.dll` (copied to game plugins).

## Smoke test

1. Open the **Market** app on the computer (or tablet market) so the shopping cart is available.
2. Confirm compact **Fill Racks** / **1 Box Empty** buttons appear as extra tabs after Vehicles (not overlapping Tools/Vehicles).
3. Click **1 Box If Empty** — products with **no boxes on labeled racks** each get 1 box added to cart.
4. Clear or buy, then click **Fill Empty Racks** — cart fills enough boxes for empty labeled rack space (capped by `MaxBoxesPerRun`).
5. Open the **tablet** or **scanner** UI — left hints for **V** / **B** should show.
6. Press **V** / **B** with cart open — same as the buttons; log shows `Tablet shortcut: ...`.
7. With cart closed, press **V** — should log/warn to open the market cart first.
8. Co-op: with 2+ players, press **V** once — should add stock once (hotkeys frame-guarded). Cart adds use `NetworkMarketShoppingCart.TryAddProduct_Request` when in a Photon room so partners see the same cart.

## Modes

- **Fill Empty Racks** (`V`): orders boxes for empty space on **labeled box rack** slots.
- **1 Box If Empty** (`B`): orders **1 box** per product that has **zero boxes on racks** (default). If `MinimumUsesBoxStockOnly` is false, empty display shelves also trigger a box.
