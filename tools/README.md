# tools/

Working PowerShell prototypes, kept as reference and as a usable fallback
while the equivalent C# lands in `AltecPrintSupport.Core`.

- **PhantomPrinter.ps1** / **PhantomPrinter.cmd** — create/remove phantom
  print queues (working v1 of the feature `Core/Printing/IPhantomPrinterService`
  is meant to replace). Self-elevating GUI + CLI.
- **printer-inventory.ps1** — dumps every printer/driver/port on the machine
  across `Get-Printer`, `Get-PrinterDriver`, WMI, and `pnputil`.
- **driver-deepdive.ps1** — per-driver deep dive: registry, INF, binary
  version info, Authenticode signature, capabilities. Reference for
  `Core/Drivers/IDriverInventoryService`.

Nothing here needs building — run directly with `powershell -ExecutionPolicy
Bypass -File <script>.ps1`.
