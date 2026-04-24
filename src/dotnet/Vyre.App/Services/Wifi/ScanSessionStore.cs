using Vyre.App.Models;

namespace Vyre.App.Services.Wifi;

public sealed class ScanSessionStore : IScanSessionStore, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ScanInsightSnapshot? _latest;

    public async Task SetLatestAsync(ScanInsightSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _latest = snapshot;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ScanInsightSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _latest;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
