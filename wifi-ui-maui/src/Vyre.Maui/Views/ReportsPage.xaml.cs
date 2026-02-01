using Vyre.Maui.ViewModels;

namespace Vyre.Maui.Views;

public partial class ReportsPage : ContentPage
{
    public ReportsPage(ReportsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
