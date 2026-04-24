using Microsoft.Maui.Controls;
using Vyre.App.ViewModels;

namespace Vyre.App.Pages;

public partial class UsagePage : ContentPage
{
    private readonly UsageViewModel _viewModel;

    public UsagePage(UsageViewModel viewModel)
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