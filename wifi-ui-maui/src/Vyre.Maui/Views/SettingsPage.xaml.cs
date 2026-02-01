using Vyre.Maui.ViewModels;

namespace Vyre.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
