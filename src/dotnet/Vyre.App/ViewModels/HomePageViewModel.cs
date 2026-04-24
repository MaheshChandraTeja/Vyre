using System.Collections.ObjectModel;
using System.Windows.Input;
using Vyre.App.Services.Engine;

namespace Vyre.App.ViewModels;

public sealed class HomePageViewModel : BaseViewModel
{
    private const string DefaultTitle = "Vyre";
    private const string DefaultModuleName = "Module 2 · Interop boundary (C ABI) + C# bindings (P/Invoke)";
    private readonly IVyreEngineService _engineService;
    private string _headline = "Checking interop boundary...";
    private string _detail = "The app is validating the C ABI seam between MAUI and the native engine.";
    private string _statusPill = "MODULE 2";
    private string _lastUpdated = "Not loaded yet.";
    private string _moduleName = DefaultModuleName;
    private string _nativeVersion = "Pending";
    private string _nativeBuildInfo = "Pending";
    private string _nativeReportJson = "Pending";
    private string _nativeLibrary = NativeMethods.LibraryName;

    public HomePageViewModel(IVyreEngineService engineService)
    {
        _engineService = engineService;
        Title = DefaultTitle;
        RefreshCommand = new Command(async () => await RefreshAsync(), () => !IsBusy);

        Checklist = new ObservableCollection<string>
        {
            "Native calls cross a flat C ABI only. No C++ objects leak into MAUI.",
            "P/Invoke bindings own string lifetimes and free native allocations explicitly.",
            "Error codes and last-error text are available without platform-specific hacks.",
            "Windows end-to-end path exercises GetVersion, scan session plumbing, and AnalyzeJson."
        };

        AccessPoints = new ObservableCollection<string>();
    }

    public string ModuleName
    {
        get => _moduleName;
        private set => SetProperty(ref _moduleName, value);
    }
    public ObservableCollection<string> Checklist { get; }
    public ObservableCollection<string> AccessPoints { get; }
    public ICommand RefreshCommand { get; }

    public string Headline
    {
        get => _headline;
        private set => SetProperty(ref _headline, value);
    }

    public string Detail
    {
        get => _detail;
        private set => SetProperty(ref _detail, value);
    }

    public string StatusPill
    {
        get => _statusPill;
        private set => SetProperty(ref _statusPill, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public string NativeVersion
    {
        get => _nativeVersion;
        private set => SetProperty(ref _nativeVersion, value);
    }

    public string NativeBuildInfo
    {
        get => _nativeBuildInfo;
        private set => SetProperty(ref _nativeBuildInfo, value);
    }

    public string NativeReportJson
    {
        get => _nativeReportJson;
        private set => SetProperty(ref _nativeReportJson, value);
    }

    public string NativeLibrary
    {
        get => _nativeLibrary;
        private set => SetProperty(ref _nativeLibrary, value);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ((Command)RefreshCommand).ChangeCanExecute();
        try
        {
            var snapshot = await _engineService.RunInteropDiagnosticsAsync(cancellationToken);

            NativeLibrary = snapshot.LibraryName;
            NativeVersion = string.IsNullOrWhiteSpace(snapshot.Version) ? "Unavailable" : snapshot.Version;
            NativeBuildInfo = string.IsNullOrWhiteSpace(snapshot.BuildInfo) ? snapshot.Message : snapshot.BuildInfo;
            NativeReportJson = string.IsNullOrWhiteSpace(snapshot.ReportJson) ? snapshot.Message : snapshot.ReportJson;

            AccessPoints.Clear();
            foreach (var accessPoint in snapshot.AccessPoints)
            {
                var name = string.IsNullOrWhiteSpace(accessPoint.Ssid) ? "<hidden>" : accessPoint.Ssid;
                AccessPoints.Add($"{name} · {accessPoint.Bssid} · ch {accessPoint.Channel} · {accessPoint.RssiDbm} dBm · {accessPoint.Security}");
            }

            if (snapshot.IsNativeAvailable)
            {
                Headline = "Interop boundary is alive.";
                Detail = snapshot.Message;
                StatusPill = "NATIVE OK";
            }
            else
            {
                Headline = "Interop boundary staged, native package missing or invalid.";
                Detail = snapshot.Message;
                StatusPill = "WAITING FOR NATIVE BUNDLE";
            }

            LastUpdated = $"Updated at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";
        }
        finally
        {
            IsBusy = false;
            ((Command)RefreshCommand).ChangeCanExecute();
        }
    }
}
