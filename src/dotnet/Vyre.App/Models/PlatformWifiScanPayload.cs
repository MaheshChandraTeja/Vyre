using System.Text.Json.Serialization;

namespace Vyre.App.Models;

public sealed class PlatformWifiScanPayload
{
    [JsonPropertyName("schema")]
    public string Schema { get; init; } = "vyre.scan.payload.v1";

    [JsonPropertyName("capturedAtUtc")]
    public DateTimeOffset CapturedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("sourcePlatform")]
    public string SourcePlatform { get; init; } = string.Empty;

    [JsonPropertyName("isPartial")]
    public bool IsPartial { get; init; }

    [JsonPropertyName("currentSsid")]
    public string? CurrentSsid { get; init; }

    [JsonPropertyName("capabilityMessage")]
    public string CapabilityMessage { get; init; } = string.Empty;

    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    [JsonPropertyName("accessPoints")]
    public IReadOnlyList<AccessPointViewData> AccessPoints { get; init; } = Array.Empty<AccessPointViewData>();
}