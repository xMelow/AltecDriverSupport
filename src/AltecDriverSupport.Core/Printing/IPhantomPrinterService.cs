namespace AltecDriverSupport.Core.Printing;

public interface IPhantomPrinterService
{
    const string PhantomComment = "PHANTOM (AltecDriverSupport)";

    IReadOnlyList<InstalledDriver> GetInstalledDrivers();
    IReadOnlyList<PhantomPrinter> GetPhantomPrinters();
    PhantomPrinter CreatePhantom(string printerName, string driverName);

    void RemovePhantom(string printerName);
    int RemoveAllPhantoms();
}

public sealed record InstalledDriver(
    string Name,
    string Provider,
    string Manufacturer,
    int MajorVersion,
    string PrinterEnvironment
);

public sealed record PhantomPrinter(string Name, string DriverName, string PortName);
