using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Vyre.App.Models;

namespace Vyre.App.Services.Diagnostics;

public sealed class NetworkDiagnosticsService : INetworkDiagnosticsService
{
    public async Task<NetworkDiagnosticSnapshot> RunAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var internetReachable = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        var dnsTimer = Stopwatch.StartNew();
        string dnsStatus;
        long? dnsLatency = null;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync("example.com", cancellationToken);
            dnsTimer.Stop();
            dnsLatency = dnsTimer.ElapsedMilliseconds;
            dnsStatus = addresses.Length > 0
                ? $"DNS resolution succeeded in {dnsLatency} ms."
                : "DNS resolution returned no addresses.";
        }
        catch (Exception ex)
        {
            dnsTimer.Stop();
            dnsStatus = $"DNS resolution failed: {ex.Message}";
        }

        var tcpTimer = Stopwatch.StartNew();
        string latencyStatus;
        long? tcpLatency = null;
        try
        {
            using var client = new TcpClient();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(TimeSpan.FromSeconds(3));

            await client.ConnectAsync("1.1.1.1", 53, linked.Token);
            tcpTimer.Stop();
            tcpLatency = tcpTimer.ElapsedMilliseconds;
            latencyStatus = $"TCP connectivity probe succeeded in {tcpLatency} ms.";
        }
        catch (Exception ex)
        {
            tcpTimer.Stop();
            latencyStatus = $"TCP connectivity probe failed: {ex.Message}";
        }

        var localInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x => x.OperationalStatus == OperationalStatus.Up)
            .Where(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToList();

        var localNetworkStatus = localInterfaces.Count > 0
            ? $"Detected {localInterfaces.Count} active network interface(s)."
            : "No active non-loopback network interfaces were detected.";

        var notes = new List<string>();

#if IOS
        notes.Add("iOS local network host discovery is intentionally conservative here. The app reports diagnostics without pretending it can enumerate nearby Wi-Fi access points.");
#endif

#if ANDROID
        notes.Add("Android diagnostics run alongside scan permissions and OS throttling rules.");
#endif

        return new NetworkDiagnosticSnapshot
        {
            InternetReachable = internetReachable,
            ReachabilityStatus = internetReachable ? "Internet reachable." : "Internet not reachable.",
            DnsStatus = dnsStatus,
            DnsLatencyMs = dnsLatency,
            LatencyStatus = latencyStatus,
            TcpLatencyMs = tcpLatency,
            LocalNetworkStatus = localNetworkStatus,
            CurrentNetworkMessage = Connectivity.Current.NetworkAccess.ToString(),
            Notes = notes
        };
    }
}