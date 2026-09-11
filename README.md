# Altec Print Support

Internal Windows tool for Altec's support team: printer, driver and NiceLabel
diagnostics for the problems that actually come up during support sessions.

## Why

Support mostly deals with RDP'd-in machines and label printers (NiceLabel /
BarTender, Altec thermal + Epson ColorWorks / ColorCube fleets). The
recurring hiccup this project starts with: opening a client's `.nlbl` label
on a machine that doesn't have the exact printer installed throws
`Unable to find printer "..."`, and picking a printer with the wrong driver
family (e.g. an Altec thermal driver for a ColorCube label) silently mangles
the label. See `docs/architecture.md` for the full roadmap and reasoning.

## Structure

```
src/
  AltecDriverSupport.Core/    class library, no UI references
    Printing/                phantom print-queue management
    Drivers/                 driver inventory & introspection
    NiceLabel/               client for the separate NiceLabelApi service
  AltecDriverSupport.App/     WPF + WPF-UI desktop app
    Views/                   XAML views
    ViewModels/              MVVM (CommunityToolkit.Mvvm)
tests/
  AltecDriverSupport.Core.Tests/
tools/                       working PowerShell prototypes (see tools/README.md)
docs/                        architecture notes
```

## Requirements

- .NET 10 SDK
- Windows (this is a Windows-only, `net10.0-windows` solution - print spooler,
  registry, WMI)

## Build & run

```bash
dotnet build
dotnet run --project src/AltecDriverSupport.App
```

## Tests

```bash
dotnet test
```
