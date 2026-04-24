using Vyre.App.Models;

namespace Vyre.App.Services.Analysis;

public sealed class AnalysisReportStore : IAnalysisReportStore, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AnalysisReportModel? _latest;

    public async Task SetLatestAsync(AnalysisReportModel report, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _latest = report;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AnalysisReportModel?> GetLatestAsync(CancellationToken cancellationToken)
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
