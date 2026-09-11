using AltecPrintSupport.Core.Printing;

namespace AltecPrintSupport.Core.Tests;

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
        // Guards the tag CreatePhantom/RemoveAllPhantoms rely on to tell a
        // phantom queue apart from a real, physical printer.
        Assert.Equal("PHANTOM (AltecPrintSupport)", IPhantomPrinterService.PhantomComment);
    }
}
