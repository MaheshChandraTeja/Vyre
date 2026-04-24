using Vyre.App.ViewModels;

namespace Vyre.App.Pages;

public partial class DoctorPage : ContentPage
{
    private readonly DoctorViewModel _viewModel;

    public DoctorPage(DoctorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}