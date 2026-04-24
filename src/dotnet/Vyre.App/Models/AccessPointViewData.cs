namespace Vyre.App.Models;

public sealed class AccessPointViewData
{
    public string Bssid { get; init; } = string.Empty;
    public string Ssid { get; init; } = string.Empty;
    public string Band { get; init; } = string.Empty;
    public int Channel { get; init; }
    public int SignalDbm { get; init; }
    public string Security { get; init; } = string.Empty;
    public bool IsOpen => string.Equals(Security, "Open", StringComparison.OrdinalIgnoreCase);
    public int FrequencyMhz { get; init; }
    public string Vendor { get; set; } = string.Empty;
    public string SecurityCategory { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public bool IsPartialObservation { get; set; }
}