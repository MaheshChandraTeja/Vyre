using System.Collections.Concurrent;

namespace Vyre.App.Services.Analysis;

public sealed class OuiVendorLookupService : IOuiVendorLookupService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _entries = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public async Task<string> LookupVendorAsync(string bssid, CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync(cancellationToken);

        var oui = NormalizeOui(bssid);
        if (string.IsNullOrWhiteSpace(oui))
        {
            return string.Empty;
        }

        return _entries.TryGetValue(oui, out var vendor) ? vendor : string.Empty;
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_loaded)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_loaded)
            {
                return;
            }

            using var stream = await FileSystem.Current.OpenAppPackageFileAsync("oui_db.csv");
            using var reader = new StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var commaIndex = line.IndexOf(',');
                if (commaIndex <= 0)
                {
                    continue;
                }

                var oui = NormalizeOui(line[..commaIndex]);
                var vendor = line[(commaIndex + 1)..].Trim();

                if (oui.Length == 6 && !string.IsNullOrWhiteSpace(vendor))
                {
                    _entries[oui] = vendor;
                }
            }

            _loaded = true;
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

    private static string NormalizeOui(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .Take(6)
            .ToArray();

        return new string(chars);
    }
}
