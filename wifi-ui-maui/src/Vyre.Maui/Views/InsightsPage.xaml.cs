using Vyre.Maui.ViewModels;

namespace Vyre.Maui.Views;

public partial class InsightsPage : ContentPage
{
    public InsightsPage(InsightsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
