# Architecture

## Why this shape

`AltecPrintSupport.Core` has **no UI references**. Every real operation —
printer/port/driver manipulation, driver introspection, talking to the
NiceLabel API — lives there as plain, testable C#. `AltecPrintSupport.App`
is a thin WPF-UI shell that binds to it. If the UI ever needs to change
(different framework, a web front end, a CLI), the engine doesn't move.

```
AltecPrintSupport.App (WPF, WPF-UI, net8.0-windows)
        │  references
        ▼
AltecPrintSupport.Core (net8.0-windows, no UI deps)
    ├─ Printing/   phantom-printer queue management
    ├─ Drivers/    driver inventory & introspection (WMI, registry, Authenticode)
    └─ NiceLabel/  HTTP client for the separate NiceLabelApi service
        │
        ▼  localhost HTTP
NiceLabelApi (.NET Framework 4.7.2, separate repo/process)
    wraps NiceLabel.SDK (SDK.NET.Interface) to read .nlbl files -
    ILabel.LabelSettings.OriginalPrinterName / OriginalPrinterDriver / Width / Height
```

## Why NiceLabelApi stays a separate process

The NiceLabel SDK is pinned to .NET Framework 4.7.2. Rather than let that
constrain the whole app, it stays behind its own HTTP API and this app talks
to it over localhost. `Core/NiceLabel/NiceLabelApiClient` is the seam - swap
the base URL or the transport without touching anything else.

## Feature roadmap (see the main README for detail)

1. **Phantom Printer** — create/remove queues so NiceLabel/BarTender labels
   bound to a printer the machine doesn't have open correctly instead of
   erroring or mangling. Working prototype: `tools/PhantomPrinter.ps1`.
2. **Label pre-flight** — read a `.nlbl` via NiceLabelApi, resolve
   `OriginalPrinterDriver` to an installed driver, offer to create the
   matching phantom directly - no pasting names, no guessing driver family.
3. **Driver info** — inventory + diagnostics (stale/unsigned/duplicate
   drivers, DriverStore cleanup, spooler/stuck-job health). Prototype:
   `tools/printer-inventory.ps1`, `tools/driver-deepdive.ps1`.

## Elevation

Printer/driver mutations need admin. The app runs as invoker and elevates
only the operations that need it (or a small worker process), rather than
requiring the whole UI to run elevated - this runs during support RDP
sessions on machines we don't control.
