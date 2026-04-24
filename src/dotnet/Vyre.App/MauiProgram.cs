using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Vyre.App.Platforms.Windows;
#endif
using Vyre.App.Pages;
using Vyre.App.Services;
using Vyre.App.Services.Engine;
using Vyre.App.Services.Wifi;
using Vyre.App.ViewModels;

namespace Vyre.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windows =>
            {
                windows.OnWindowCreated(WindowsWindowChrome.Apply);
            });
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<IDummyWifiDataService, DummyWifiDataService>();
        builder.Services.AddSingleton<IDoctorService, DoctorService>();
        builder.Services.AddSingleton<IVyreEngineService, VyreEngineService>();
        builder.Services.AddSingleton<IWifiScanService, WifiScanService>();

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

        builder.Services.AddSingleton<Vyre.App.Services.Wifi.IPlatformWifiScanProvider, Vyre.App.Services.Wifi.PlatformWifiScanProvider>();
        builder.Services.AddSingleton<Vyre.App.Services.Wifi.IScanSessionStore, Vyre.App.Services.Wifi.ScanSessionStore>();
        builder.Services.AddSingleton<Vyre.App.Services.Diagnostics.INetworkDiagnosticsService, Vyre.App.Services.Diagnostics.NetworkDiagnosticsService>();

        builder.Services.AddSingleton<Vyre.App.Services.Wifi.IWifiScanService, Vyre.App.Services.Wifi.WifiScanService>();

        builder.Services.AddSingleton<Vyre.App.Services.Analysis.IOuiVendorLookupService, Vyre.App.Services.Analysis.OuiVendorLookupService>();
        builder.Services.AddSingleton<Vyre.App.Services.Analysis.IWifiNormalizationService, Vyre.App.Services.Analysis.WifiNormalizationService>();
        builder.Services.AddSingleton<Vyre.App.Services.Analysis.IWifiAnalyzerService, Vyre.App.Services.Analysis.WifiAnalyzerService>();
        builder.Services.AddSingleton<Vyre.App.Services.Analysis.IAnalysisReportStore, Vyre.App.Services.Analysis.AnalysisReportStore>();
        builder.Services.AddSingleton<Vyre.App.Services.Analysis.IScanAnalysisPipeline, Vyre.App.Services.Analysis.ScanAnalysisPipeline>();

        builder.Services.AddSingleton<Vyre.App.Services.Reports.IReportArchiveService, Vyre.App.Services.Reports.ReportArchiveService>();
        builder.Services.AddSingleton<Vyre.App.Services.Capture.IPacketCaptureService, Vyre.App.Services.Capture.PacketCaptureService>();

        builder.Services.AddTransient<Vyre.App.ViewModels.CaptureViewModel>();
        builder.Services.AddTransient<Vyre.App.Pages.CapturePage>();

        builder.Services.AddSingleton<Vyre.App.Services.Usage.IAppNetworkUsageService, Vyre.App.Services.Usage.AppNetworkUsageService>();
        builder.Services.AddTransient<Vyre.App.ViewModels.UsageViewModel>();
        builder.Services.AddTransient<Vyre.App.Pages.UsagePage>();

        builder.Services.AddTransient<Vyre.App.Pages.MorePage>();
        
        return builder.Build();
    }
}
