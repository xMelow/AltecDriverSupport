using System.Net.Http.Json;

namespace AltecPrintSupport.Core.NiceLabel;

/// <summary>
/// Thin HTTP client for the existing AltecTools NiceLabelApi service (.NET
/// Framework 4.7.2, wraps NiceLabel.SDK - see ILabel/ILabelSettings). Kept as
/// a separate out-of-process service because the NiceLabel SDK is pinned to
/// .NET Framework; this app talks to it over localhost instead of referencing
/// it directly.
/// </summary>
public sealed class NiceLabelApiClient(HttpClient httpClient) : INiceLabelApiClient
{
    public async Task<LabelInfo> GetLabelInfoAsync(string labelFilePath, CancellationToken ct = default)
    {
        // TODO: confirm the actual NiceLabelApi route/contract once it's finalized.
        // Expected to read ILabel.LabelSettings.OriginalPrinterName / OriginalPrinterDriver
        // (and Width/Height, 0.001mm units) via the wrapped NiceLabel SDK.
        var response = await httpClient.PostAsJsonAsync("api/labels/info",
            new { path = labelFilePath }, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LabelInfo>(cancellationToken: ct)
            ?? throw new InvalidOperationException("NiceLabelApi returned an empty response.");
    }
}

public interface INiceLabelApiClient
{
    Task<LabelInfo> GetLabelInfoAsync(string labelFilePath, CancellationToken ct = default);
}

/// <summary>Mirrors NiceLabel.SDK.ILabelSettings for the fields we actually need.</summary>
public sealed record LabelInfo(
    string OriginalPrinterName,
    string OriginalPrinterDriver,
    int WidthThousandthsMm,   // ILabelSettings.Width: "label width in 0.001mm units"
    int HeightThousandthsMm,  // ILabelSettings.Height: same units
    IReadOnlyList<string> ConversionIssues);
