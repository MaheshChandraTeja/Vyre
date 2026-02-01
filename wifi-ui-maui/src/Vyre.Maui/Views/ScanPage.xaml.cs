using Vyre.Maui.ViewModels;

namespace Vyre.Maui.Views;

public partial class ScanPage : ContentPage
{
    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.RefreshAsync();
    }
}
