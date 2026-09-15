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

    [Fact]
    public void CreatePhantom_Throws_WhenDriverNotInstalled()
    {
        Assert.Throws<InvalidOperationException>(() => _service.CreatePhantom("Phantom printer", "driver"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreatePhantom_Throws_WhenPrinterNameIsBlank(string blankName)
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));

        Assert.Throws<ArgumentException>(() => _service.CreatePhantom(blankName, "driver"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreatePhantom_Throws_WhenDriverNameIsBlank(string blankName)
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));

        Assert.Throws<ArgumentException>(() => _service.CreatePhantom("phantom printer", blankName));
    }

    [Fact]
    public void CreatePhantom_Throws_WhenPrinterNameIsNull()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));

        Assert.Throws<ArgumentNullException>(() => _service.CreatePhantom(null!, "driver"));
    }

    [Fact]
    public void CreatePhantom_Throws_WhenDriverNameIsNull()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));

        Assert.Throws<ArgumentNullException>(() => _service.CreatePhantom("phantom printer", null!));
    }

    [Fact]
    public void RemovePhantom_RemovesQueue_WhenItIsAPhantom()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));

        _service.RemovePhantom("Phantom Printer");

        Assert.Empty(_spooler.Printers);
    }

    [Fact]
    public void RemovePhantom_Throws_WhenNameIsNotAPhantom()
    {
        Assert.Throws<InvalidOperationException>(() => _service.RemovePhantom("Nonexistent"));
    }

    [Fact]
    public void RemovePhantom_OnlyDeletesPort_WhenNoOtherPhantomUsesIt()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer 2", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Ports.Add("altec port");

        _service.RemovePhantom("Phantom printer");

        Assert.Contains(_spooler.Ports, p => p == "altec port");

        _service.RemovePhantom("Phantom printer 2");

        Assert.Empty(_spooler.Ports);
    }

    [Fact]
    public void RemoveAllPhantom_RemovesQueue_WhenItIsAPhantom()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer 2", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Printers.Add(new SpoolerPrinter("printer", "driver", "USB", "Desktop printer"));

        var removedCount = _service.RemoveAllPhantoms();

        Assert.Contains(_spooler.Printers, p => p.Name == "printer");
        Assert.Equal(2, removedCount);
    }

    [Fact]
    public void RemoveAllPhantoms_ReturnsZero_WhenThereAreNoPhantoms()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("printer", "driver", "USB", "Desktop printer"));

        var removedCount = _service.RemoveAllPhantoms();

        Assert.Equal(0, removedCount);
    }

    [Fact]
    public void RemoveAllPhantoms_RemovesPorts_WhenAllPhantomPrintersCleared()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer 2", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Ports.Add("altec port");

        var removedCount = _service.RemoveAllPhantoms();

        Assert.Equal(2, removedCount);
        Assert.Empty(_spooler.Ports);
    }

    [Fact]
    public void GetPhantomPrinters_ExcludesRealPrinters()
    {
        _spooler.Drivers.Add(new InstalledDriver("driver", "SomeProvider", "SomeManufacturer", 3, "Windows x64"));
        _spooler.Printers.Add(new SpoolerPrinter("Phantom printer", "driver", "altec port", IPhantomPrinterService.PhantomComment));
        _spooler.Printers.Add(new SpoolerPrinter("printer", "driver", "USB", "Desktop printer"));
    
        var phantomPrinters = _service.GetPhantomPrinters();
    
        Assert.Single(phantomPrinters);
        Assert.Contains(phantomPrinters, p => p.Name == "Phantom printer");
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
