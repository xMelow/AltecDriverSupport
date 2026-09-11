namespace AltecPrintSupport.Core.Printing;

/// <summary>
/// Creates and removes "phantom" print queues: local queues bound to a real,
/// installed driver but pointed at a null port, so a NiceLabel/BarTender label
/// bound to a printer the machine doesn't have will open (and render correctly)
/// instead of erroring or being remapped onto an incompatible driver.
///
/// Every queue this service creates is tagged (see <see cref="PhantomComment"/>)
/// so removal can never touch a real, physical printer.
/// </summary>
public interface IPhantomPrinterService
{
    /// <summary>Comment string stamped on every queue this service creates.</summary>
    const string PhantomComment = "PHANTOM (AltecPrintSupport)";

    /// <summary>Lists every driver currently installed on this machine.</summary>
    IReadOnlyList<InstalledDriver> GetInstalledDrivers();

    /// <summary>Lists every phantom queue this service has created.</summary>
    IReadOnlyList<PhantomPrinter> GetPhantomPrinters();

    /// <summary>
    /// Creates a phantom queue named <paramref name="printerName"/> bound to
    /// <paramref name="driverName"/>. Throws if a queue with that name already
    /// exists (real or phantom), or if the driver isn't installed.
    /// </summary>
    PhantomPrinter CreatePhantom(string printerName, string driverName);

    /// <summary>Removes one phantom queue by name. No-op if it isn't a phantom.</summary>
    void RemovePhantom(string printerName);

    /// <summary>Removes every phantom queue this service has created.</summary>
    int RemoveAllPhantoms();
}

public sealed record InstalledDriver(
    string Name,
    string Provider,       // e.g. "NiceLabel" vs "Seagull" vs "hp" - distinguishes driver families
    string Manufacturer,
    int MajorVersion,      // 3 = legacy, 4 = packaged/v4
    string PrinterEnvironment);

public sealed record PhantomPrinter(string Name, string DriverName, string PortName);
