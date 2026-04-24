using Vyre.App.Services.Usage;

namespace Vyre.App.Pages;

public partial class MorePage : ContentPage
{
    private readonly IAppNetworkUsageService _usageService;

    public MorePage(IAppNetworkUsageService usageService)
    {
        InitializeComponent();

        _usageService = usageService;

        UsageCard.IsVisible = _usageService.ShouldShowTab;
        UsageUnavailableCard.IsVisible = !_usageService.ShouldShowTab;
    }

    private async void OnUsageTapped(object sender, TappedEventArgs e)
    {
        if (!_usageService.ShouldShowTab)
        {
            return;
        }

        await Shell.Current.GoToAsync("usage");
    }

    private async void OnDoctorTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("doctor");
    }

    private async void OnSettingsTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("settings");
    }
}
