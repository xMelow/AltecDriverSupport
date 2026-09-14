using AltecDriverSupport.Core.Printing;

namespace AltecDriverSupport.Core.Tests;

/// <summary>
/// Placeholder proving the test project references Core correctly. Real
/// tests land once IPhantomPrinterService has an implementation - given it
/// talks to the Win32 print spooler, that implementation should be exercised
/// through a fake/wrapper interface here rather than hitting the real
/// spooler in unit tests.
/// </summary>
public class PhantomPrinterServiceTests
{
    [Fact]
    public void PhantomComment_IsStableAndDistinctive()
    {
        Assert.Equal("PHANTOM (AltecDriverSupport)", IPhantomPrinterService.PhantomComment);
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
