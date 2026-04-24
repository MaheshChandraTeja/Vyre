using Vyre.App.Models;

namespace Vyre.App.Services;

public interface ISettingsService
{
    Task<AppSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}