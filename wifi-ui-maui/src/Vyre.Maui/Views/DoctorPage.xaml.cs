using Vyre.Maui.ViewModels;

namespace Vyre.Maui.Views;

public partial class DoctorPage : ContentPage
{
    public DoctorPage(DoctorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
