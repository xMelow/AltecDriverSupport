using AltecDriverSupport.Core.Printing;

namespace AltecDriverSupport.Core.Tests;

/// <summary>
/// Exercises PhantomPrinterService's rules against a FakeSpooler instead of
/// the real Win32 spooler - see Win32PrintSpooler for the actual winspool.drv
/// calls once that's built.
/// </summary>
public class PhantomPrinterServiceTests
{
    private readonly FakeSpooler _spooler = new();
    private readonly PhantomPrinterService _service;

    public PhantomPrinterServiceTests()
    {
        _service = new PhantomPrinterService(_spooler);
    }

    [Fact]
    public void PhantomComment_IsStableAndDistinctive()
    {
        Assert.Equal("PHANTOM (AltecDriverSupport)", IPhantomPrinterService.PhantomComment);
    }

    [Fact]
    public void CreatePhantom_AddsPrinter_WhenDriverIsInstalled()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));

        _service.CreatePhantom("Test", "driver");

        Assert.Contains(_service.GetPhantomPrinters(), item => item.Name == "Test");
    }

    [Fact]
    public void CreatePhantom_Throws_WhenNameCollidesWithRealPrinter()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Test printer", "driver", "altec port", ""));

        Assert.Throws<InvalidOperationException>(() => _service.CreatePhantom("Test printer", "driver"));
    }

    [Fact]
    public void CreatePhantom_Throws_WhenNameCollidesWithPhantomPrinter()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));

        Assert.Throws<InvalidOperationException>(() => _service.CreatePhantom("Phantom printer", "driver"));
    }

    private sealed class FakeSpooler : IWin32PrintSpooler
    {
        public List<InstalledDriver> Drivers { get; } = [];
        public List<SpoolerPrinter> Printers { get; } = [];
        public List<string> Ports { get; } = [];

        public IReadOnlyList<InstalledDriver> EnumDrivers() => Drivers.AsReadOnly();
        public IReadOnlyList<SpoolerPrinter> EnumPrinters() => Printers.AsReadOnly();
        public void AddPort(string portName)
        {
            if (!Ports.Contains(portName)) Ports.Add(portName);
        }
        public void AddPrinter(string printerName, string driverName, string portName, string comment) => Printers.Add(new SpoolerPrinter(printerName, driverName, portName, comment));
        public void DeletePrinter(string printerName) => Printers.RemoveAll(p => p.Name == printerName);
        public void DeletePort(string portName) => Ports.RemoveAll(p => p == portName);
    }
}
