namespace Vyre.App.Services.Analysis;

public interface IOuiVendorLookupService
{
    Task<string> LookupVendorAsync(string bssid, CancellationToken cancellationToken);
}