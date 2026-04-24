using Vyre.App.Models;

namespace Vyre.App.Services.Engine;

public interface IVyreEngineService
{
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task<NativeInteropSnapshot> RunInteropDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<string> SubmitScanResultsJsonAsync(string scanResultsJson, CancellationToken cancellationToken = default);
}
