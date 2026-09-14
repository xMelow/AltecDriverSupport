namespace AltecDriverSupport.Core.Printing;

internal interface IWin32PrintSpooler
{
    IReadOnlyList<InstalledDriver> EnumDrivers();
    IReadOnlyList<SpoolerPrinter> EnumPrinters();

    void AddPort(string portName);
    void AddPrinter(string printerName, string driverName, string portName, string comment);
    void DeletePrinter(string printerName);
    void DeletePort(string portName);
}

internal sealed record SpoolerPrinter(string Name, string DriverName, string PortName, string? Comment);
