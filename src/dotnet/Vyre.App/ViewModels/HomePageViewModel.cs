using System.Collections.ObjectModel;
using System.Windows.Input;
using Vyre.App.Core.Models;
using Vyre.App.Core.Services;
using Vyre.App.Services.Engine;

namespace Vyre.App.ViewModels;

public sealed class HomePageViewModel : BaseViewModel
{
    private readonly IVyreEngineService _engineService;
    private bool _isBusy;
    private string _headline = "Checking native bridge...";
    private string _detail = "The app shell is alive. We are validating the managed/native seam.";
    private string _statusPill = "BOOTSTRAP";
    private string _lastUpdated = "Not loaded yet.";

    public HomePageViewModel(IVyreEngineService engineService)
    {
        _engineService = engineService;
        RefreshCommand = new Command(async () => await RefreshAsync(), () => !IsBusy);

        Checklist = new ObservableCollection<string>
        {
            "Native engine is isolated under src/native and builds with CMake presets.",
            "Interop is the only supported boundary between MAUI and C++.",
            "Managed shared logic sits in Vyre.App.Core, not in page code-behind.",
            "VS Code tasks can bootstrap, build, test, and validate the repo in one place."
        };
    }

    public string Title => "Vyre";
    public string ModuleName => "Module 1 · Monorepo Foundation, Build System, and Dev Workflow";
    public ObservableCollection<string> Checklist { get; }
    public ICommand RefreshCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ((Command)RefreshCommand).ChangeCanExecute();
            }
        }
    }

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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var buildInfo = await _engineService.GetBuildInfoAsync(cancellationToken);
            var status = new EngineBridgeStatus(
                buildInfo.IsNativeAvailable,
                buildInfo.Source,
                buildInfo.LibraryName,
                buildInfo.Message);

            Headline = BootstrapStatusFormatter.FormatHeadline(status);
            Detail = BootstrapStatusFormatter.FormatDetail(status);
            StatusPill = buildInfo.IsNativeAvailable ? "NATIVE READY" : "BRIDGE STAGED";
            LastUpdated = $"Updated at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
