using Vyre.App.Models;

namespace Vyre.App.Services.Diagnostics;

public interface INetworkDiagnosticsService
{
    Task<NetworkDiagnosticSnapshot> RunAsync(CancellationToken cancellationToken);
}