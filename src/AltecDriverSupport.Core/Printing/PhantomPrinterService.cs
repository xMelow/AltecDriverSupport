namespace AltecDriverSupport.Core.Printing;

public sealed class PhantomPrinterService : IPhantomPrinterService
{
    private const string PortName = "AltecDriverSupport_NUL:";

    private readonly IWin32PrintSpooler _spooler;

    internal PhantomPrinterService(IWin32PrintSpooler spooler)
    {
        _spooler = spooler;
    }

    public IReadOnlyList<InstalledDriver> GetInstalledDrivers() => _spooler.EnumDrivers();

    public IReadOnlyList<PhantomPrinter> GetPhantomPrinters() =>
        _spooler.EnumPrinters()
            .Where(p => p.Comment == IPhantomPrinterService.PhantomComment)
            .Select(p => new PhantomPrinter(p.Name, p.DriverName, p.PortName))
            .ToList();

    public PhantomPrinter CreatePhantom(string printerName, string driverName)
    {
        var existing = _spooler.EnumPrinters()
            .FirstOrDefault(p => string.Equals(p.Name, printerName, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            throw existing.Comment == IPhantomPrinterService.PhantomComment
                ? new InvalidOperationException($"A phantom named '{printerName}' already exists.")
                : new InvalidOperationException($"'{printerName}' already exists as a REAL printer.");
        }

        var driverInstalled = _spooler.EnumDrivers()
            .Any(d => string.Equals(d.Name, driverName, StringComparison.OrdinalIgnoreCase));

        if (!driverInstalled)
            throw new InvalidOperationException($"Driver '{driverName}' is not installed on this machine.");

        _spooler.AddPort(PortName);
        _spooler.AddPrinter(printerName, driverName, PortName, IPhantomPrinterService.PhantomComment);

        return new PhantomPrinter(printerName, driverName, PortName);
    }

    public void RemovePhantom(string printerName)
    {
        var phantom = _spooler.EnumPrinters()
            .FirstOrDefault(p => string.Equals(p.Name, printerName, StringComparison.OrdinalIgnoreCase)
                                  && p.Comment == IPhantomPrinterService.PhantomComment);

        if (phantom is null)
            throw new InvalidOperationException($"'{printerName}' is not a phantom printer.");

        _spooler.DeletePrinter(phantom.Name);
        RemovePortIfUnused(phantom.PortName);
    }

    public int RemoveAllPhantoms()
    {
        var phantoms = GetPhantomPrinters();

        foreach (var phantom in phantoms)
            _spooler.DeletePrinter(phantom.Name);

        foreach (var port in phantoms.Select(p => p.PortName).Distinct(StringComparer.OrdinalIgnoreCase))
            RemovePortIfUnused(port);

        return phantoms.Count;
    }

    private void RemovePortIfUnused(string portName)
    {
        var stillUsed = _spooler.EnumPrinters()
            .Any(p => string.Equals(p.PortName, portName, StringComparison.OrdinalIgnoreCase));

        if (!stillUsed)
            _spooler.DeletePort(portName);
    }
}
