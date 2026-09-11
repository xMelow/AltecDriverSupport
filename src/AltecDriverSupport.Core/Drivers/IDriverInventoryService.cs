namespace AltecDriverSupport.Core.Drivers;

/// <summary>
/// Read-only introspection of installed print drivers: spooler metadata,
/// registry attributes (isolation, package-awareness, hardware IDs), the
/// underlying binary's version info, and its Authenticode signature.
///
/// This is the "driver info" surface explored in docs/driver-data-model.md -
/// not yet implemented, kept as its own service so it stays independent of
/// the phantom-printer feature.
/// </summary>
public interface IDriverInventoryService
{
    IReadOnlyList<DriverDetails> GetAllDrivers();

    DriverDetails? GetDriver(string driverName);
}

public sealed record DriverDetails(
    string Name,
    string Manufacturer,
    string Provider,
    int MajorVersion,
    string PrinterEnvironment,
    string? InfPath,
    string? DriverBinaryPath,
    string? FileVersion,
    DateTime? DriverDate,
    bool IsPackageAware,
    string? HardwareId,
    SignatureInfo? Signature);

public sealed record SignatureInfo(
    string Status,
    string SignatureType,     // "Catalog" or "Embedded" --> change to enum?
    string? SignerSubject,
    DateTime? NotAfter);
