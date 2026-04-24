using System.Text.Json;
using Vyre.App.Models;

namespace Vyre.App.Services;

public sealed class SettingsService : ISettingsService
{
    private const string PreferencesKey = "vyre.settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public Task<AppSettings> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = Preferences.Default.Get(PreferencesKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult(new AppSettings());
        }

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return Task.FromResult(settings ?? new AppSettings());
        }
        catch
        {
            return Task.FromResult(new AppSettings());
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        Preferences.Default.Set(PreferencesKey, json);
        return Task.CompletedTask;
    }
}