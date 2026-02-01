using Microsoft.Extensions.Logging;
using Vyre.Maui.Services;
using Vyre.Maui.ViewModels;
using Vyre.Maui.Views;

namespace Vyre.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        #if DEBUG
            builder.Logging.AddDebug();
        #endif

        #if WINDOWS
            builder.Services.AddSingleton<IWifiEngine, WindowsWifiEngine>();
        #else
            builder.Services.AddSingleton<IWifiEngine, DummyWifiEngine>();
        #endif

        builder.Services.AddSingleton<ISettingsStore, PreferencesSettingsStore>();
        builder.Services.AddSingleton<IReportStore, FileReportStore>();

        builder.Services.AddTransient<ScanViewModel>();
        builder.Services.AddTransient<InsightsViewModel>();
        builder.Services.AddTransient<ReportsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<DoctorViewModel>();

        builder.Services.AddTransient<ScanPage>();
        builder.Services.AddTransient<InsightsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<DoctorPage>();

        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
