namespace Vyre.App.Models;

public sealed class NetworkDiagnosticSnapshot
{
    public bool InternetReachable { get; init; }
    public string ReachabilityStatus { get; init; } = string.Empty;
    public string DnsStatus { get; init; } = string.Empty;
    public long? DnsLatencyMs { get; init; }
    public string LatencyStatus { get; init; } = string.Empty;
    public long? TcpLatencyMs { get; init; }
    public string LocalNetworkStatus { get; init; } = string.Empty;
    public string CurrentNetworkMessage { get; init; } = string.Empty;
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}