using Vyre.Maui.Models;

namespace Vyre.Maui.Services;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}
