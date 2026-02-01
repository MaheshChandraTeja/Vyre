using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vyre.Maui.Services;

namespace Vyre.Maui.ViewModels;

public partial class DoctorViewModel : BaseViewModel
{
    private readonly IWifiEngine _engine;

    [ObservableProperty] private string nativeVersion = "";
    [ObservableProperty] private string permissionsStatus = "Unknown";
    [ObservableProperty] private string capabilitiesStatus = "Unknown";

    public DoctorViewModel(IWifiEngine engine) => _engine = engine;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            NativeVersion = await _engine.GetNativeVersionAsync(CancellationToken.None);

            permissionsStatus = "OK (dummy)";
            capabilitiesStatus = "OK (dummy)";

            StatusMessage = "Doctor check complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Doctor failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
