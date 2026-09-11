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
}
